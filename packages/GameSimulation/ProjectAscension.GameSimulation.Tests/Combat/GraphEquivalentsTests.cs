using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using Xunit;
using EffectSpread = ProjectAscension.GameSimulation.Effects.Spread;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>
    /// The graph analogues of the primitive-derived subsystems (ADR 0007 Phase 4c) — passive
    /// defense, focus cost, knowledge value, VFX accents. They must match the primitive path's
    /// behaviour so switching consumers to graph-first is non-regressive.
    /// </summary>
    public class GraphEquivalentsTests
    {
        private static EffectNode Cast(params EffectNode[] steps)
            => new Trigger(TriggerKind.OnCast, steps.Length == 1 ? steps[0] : new Sequence(steps));

        [Fact]
        public void GraphPassive_MatchesPrimitivePassive_ForEquivalentWards()
        {
            // Ward(Barrier, tier1 → mag 2) + Ward(Leech, tier2 → mag 3), same magnitudes the
            // primitive test uses (Barrier 2, Leech 3).
            var graph = new Trigger(TriggerKind.Continuous,
                new Sequence(new EffectNode[] { new Ward(WardEffect.Barrier, 1), new Ward(WardEffect.Leech, 2) }));
            var effect = GraphPassiveResolver.Resolve(graph);

            Assert.Equal(0.16f, effect.DamageReduction, precision: 3); // Barrier 2 × 0.08
            Assert.Equal(0.15f, effect.Lifesteal, precision: 3);       // Leech 3 × 0.05
        }

        [Fact]
        public void GraphPassive_OffenseAndMovement_DoNotContribute()
        {
            var graph = Cast(new Emit(EmitDelivery.Projectile, 3), new Impulse(ImpulseDirection.Up, 3));
            Assert.Equal(PassiveEffect.None, GraphPassiveResolver.Resolve(graph));
        }

        [Fact]
        public void PowerPoints_GrowWithTierAndSize()
        {
            int small = EffectGraphQuery.PowerPoints(Cast(new Emit(EmitDelivery.Projectile, 0)));
            int bigTier = EffectGraphQuery.PowerPoints(Cast(new Emit(EmitDelivery.Projectile, 3)));
            int moreNodes = EffectGraphQuery.PowerPoints(Cast(new Emit(EmitDelivery.Projectile, 0), new Damage(0), new EffectSpread(0)));

            Assert.True(bigTier > small);     // higher tier → more power
            Assert.True(moreNodes > small);   // more nodes → more power
        }

        [Fact]
        public void FocusCost_FromGraph_IsPowerPointsTimesRate()
        {
            var graph = Cast(new Emit(EmitDelivery.Beam, 2), new Damage(1));
            int points = EffectGraphQuery.PowerPoints(graph);
            Assert.Equal(points * CombatTuning.Default.FocusCostPerPoint, FocusCost.Of(graph), precision: 3);
        }

        [Fact]
        public void KnowledgeValue_FromGraph_TracksPower()
        {
            var weak = Cast(new Emit(EmitDelivery.Projectile, 0));
            var strong = Cast(new Emit(EmitDelivery.Nova, 3), new Damage(3), new Dot(3, 4));
            Assert.True(KnowledgeValuation.PowerPoints(strong) > KnowledgeValuation.PowerPoints(weak));
        }

        [Fact]
        public void AccentFlags_ReflectTheGraph()
        {
            var graph = Cast(new Emit(EmitDelivery.Projectile, 1), new EffectSpread(1),
                new Control(ControlEffect.Knockback, 1), new Ward(WardEffect.Leech, 1), new Dot(2, 3));
            Assert.True(EffectGraphQuery.HasSpread(graph));
            Assert.True(EffectGraphQuery.HasKnockback(graph));
            Assert.True(EffectGraphQuery.HasLeech(graph));
            Assert.Equal(3, EffectGraphQuery.MaxDotDuration(graph));

            var plain = Cast(new Emit(EmitDelivery.Beam, 1));
            Assert.False(EffectGraphQuery.HasSpread(plain));
            Assert.False(EffectGraphQuery.HasKnockback(plain));
            Assert.Equal(0, EffectGraphQuery.MaxDotDuration(plain));
        }
    }
}
