using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using Xunit;
using EffectSpread = ProjectAscension.GameSimulation.Effects.Spread;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>
    /// The legacy primitive→graph translator (ADR 0007 Phase 4c-4) — a graphless skill must become
    /// an equivalent graph deterministically, so it runs on the graph path like any other and the
    /// runtime keeps ONE code path. Checks the trigger taxonomy and that translated graphs resolve.
    /// </summary>
    public class PrimitiveGraphTranslatorTests
    {
        private static Skill Of(params SkillPrimitive[] prims) => new("Legacy", prims);

        [Fact]
        public void Offensive_BecomesOnCast_WithMatchingNodes()
        {
            var graph = PrimitiveGraphTranslator.Translate(Of(
                new SkillPrimitive(SkillPrimitiveKind.Projectile, 3),
                new SkillPrimitive(SkillPrimitiveKind.Chain, 2),
                new SkillPrimitive(SkillPrimitiveKind.DamageOverTime, 1, Duration: 2)));

            var trigger = Assert.IsType<Trigger>(graph);
            Assert.Equal(TriggerKind.OnCast, trigger.Kind);
            // Resolving it must deal damage (it's a real offensive skill).
            var res = GraphSkillResolver.Resolve(graph, availableTargets: 3, CombatTuning.Default);
            Assert.True(res.ImmediateDamage > 0f);
        }

        [Fact]
        public void Mobility_BecomesAMovementTrigger()
        {
            var graph = PrimitiveGraphTranslator.Translate(Of(
                new SkillPrimitive(SkillPrimitiveKind.Dash, 2), new SkillPrimitive(SkillPrimitiveKind.Blink, 1)));
            Assert.Equal(TriggerKind.OnJumpInAir, Assert.IsType<Trigger>(graph).Kind);
            // ...and grants an extra jump through the same MovementCapability path.
            Assert.Equal(1, ProjectAscension.GameSimulation.Player.MovementCapability.From(new[] { graph }).ExtraJumps);
        }

        [Fact]
        public void Defensive_BecomesContinuousWard()
        {
            var graph = PrimitiveGraphTranslator.Translate(Of(
                new SkillPrimitive(SkillPrimitiveKind.Shield, 3), new SkillPrimitive(SkillPrimitiveKind.Leech, 1)));
            Assert.Equal(TriggerKind.Continuous, Assert.IsType<Trigger>(graph).Kind);
            var passive = GraphPassiveResolver.Resolve(graph);
            Assert.True(passive.DamageReduction > 0f); // shield reduces damage
        }

        [Fact]
        public void ChainForkPierce_AllMapToSpread()
        {
            foreach (var kind in new[] { SkillPrimitiveKind.Chain, SkillPrimitiveKind.Fork, SkillPrimitiveKind.Pierce })
            {
                var graph = PrimitiveGraphTranslator.Translate(Of(
                    new SkillPrimitive(SkillPrimitiveKind.Projectile, 2), new SkillPrimitive(kind, 1)));
                Assert.True(EffectGraphQuery.HasSpread(graph), $"{kind} should translate to Spread");
            }
        }

        [Fact]
        public void EmptySkill_YieldsAHarmlessCast()
        {
            var graph = PrimitiveGraphTranslator.Translate(new Skill("Empty", System.Array.Empty<SkillPrimitive>()));
            Assert.IsType<Trigger>(graph);
            Assert.Empty(GraphSkillResolver.Resolve(graph, 3, CombatTuning.Default).Hits);
        }
    }
}
