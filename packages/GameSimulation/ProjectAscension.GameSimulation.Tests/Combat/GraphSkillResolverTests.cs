using System.Linq;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using Xunit;
using EffectSpread = ProjectAscension.GameSimulation.Effects.Spread;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class GraphSkillResolverTests
    {
        private static EffectNode Cast(params EffectNode[] steps)
            => new Trigger(TriggerKind.OnCast, steps.Length == 1 ? steps[0] : new Sequence(steps));

        [Fact]
        public void NonTriggerOrNoTargets_IsEmpty()
        {
            Assert.Equal(SkillResolution.Empty, GraphSkillResolver.Resolve(new Damage(1), 3));
            Assert.Equal(SkillResolution.Empty, GraphSkillResolver.Resolve(Cast(new Emit(EmitDelivery.Projectile, 1)), 0));
        }

        [Fact]
        public void Projectile_HitsThePrimaryOnly()
        {
            var res = GraphSkillResolver.Resolve(Cast(new Emit(EmitDelivery.Projectile, 1)), availableTargets: 3);
            Assert.Single(res.Hits);
            Assert.True(res.Hits[0].Damage > 0f);
        }

        [Fact]
        public void Burst_IsAreaAndHitsEveryTarget()
        {
            var res = GraphSkillResolver.Resolve(Cast(new Emit(EmitDelivery.Burst, 2)), availableTargets: 3);
            Assert.Equal(3, res.Hits.Count);
            Assert.All(res.Hits, h => Assert.True(h.Damage > 0f));
        }

        [Fact]
        public void Spread_ReachesExtraTargets_WithFalloff()
        {
            var res = GraphSkillResolver.Resolve(
                Cast(new Emit(EmitDelivery.Projectile, 2), new EffectSpread(0)), availableTargets: 5);
            Assert.Equal(2, res.Hits.Count);                       // primary + 1 spread (tier 0 → +1)
            Assert.True(res.Hits[1].Damage < res.Hits[0].Damage);  // falloff on the chained target
        }

        [Fact]
        public void Dot_AddsAStreamWithTicks()
        {
            var res = GraphSkillResolver.Resolve(Cast(new Emit(EmitDelivery.Projectile, 1), new Dot(2, 3)), availableTargets: 1);
            Assert.True(res.Hits[0].DamageOverTimePerTick > 0f);
            Assert.True(res.Hits[0].DamageOverTimeTicks > 0);
        }

        [Fact]
        public void Control_AppliesTheStrongest()
        {
            var res = GraphSkillResolver.Resolve(
                Cast(new Emit(EmitDelivery.Projectile, 1), new Control(ControlEffect.Knockback, 1), new Control(ControlEffect.Stun, 2)),
                availableTargets: 1);
            Assert.Equal(ControlKind.Stun, res.Hits[0].Control); // Stun > Knockback
            Assert.True(res.Hits[0].ControlDuration > 0f);
        }

        [Fact]
        public void WardLeech_HealsTheCaster_WardShield_Shields()
        {
            var leech = GraphSkillResolver.Resolve(
                Cast(new Emit(EmitDelivery.Projectile, 2), new Ward(WardEffect.Leech, 2)), availableTargets: 1);
            Assert.True(leech.SelfHeal > 0f);

            var shield = GraphSkillResolver.Resolve(Cast(new Ward(WardEffect.Shield, 2)), availableTargets: 1);
            Assert.True(shield.SelfShield > 0f);
        }

        [Fact]
        public void Homing_ContributesNoNumbers()
        {
            var withHoming = GraphSkillResolver.Resolve(Cast(new Emit(EmitDelivery.Projectile, 1), new Homing(3)), availableTargets: 1);
            var without = GraphSkillResolver.Resolve(Cast(new Emit(EmitDelivery.Projectile, 1)), availableTargets: 1);
            Assert.Equal(without.ImmediateDamage, withHoming.ImmediateDamage, precision: 3);
        }

        [Fact]
        public void ProjectileThatChainsAndKnocksBack_HasTheExpectedShape()
        {
            // A projectile that chains once (Spread tier 0 → +1 target) and knocks back — primary
            // plus one chained hit, the primary carrying the knockback control.
            var graph = GraphSkillResolver.Resolve(
                Cast(new Emit(EmitDelivery.Projectile, 1), new EffectSpread(0), new Control(ControlEffect.Knockback, 1)),
                availableTargets: 5);

            Assert.Equal(2, graph.Hits.Count);                          // primary + 1 spread
            Assert.Equal(ControlKind.Knockback, graph.Hits[0].Control);
            Assert.True(graph.Hits[0].Damage > 0f);
            Assert.True(graph.Hits[1].Damage < graph.Hits[0].Damage);   // falloff on the chained target
        }
    }
}
