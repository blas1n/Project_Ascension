using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    // The combat numbers are no longer hard-coded: a CombatTuning (DB-driven in the host)
    // reshapes resolver output. Critical for the weapon-creation system — a balance edit
    // changes every discovered weapon's combat result without touching code. Resolved off the
    // effect GRAPH now (ADR 0007 Phase 4c); tier + 1 stands in for the old primitive magnitude.
    public class CombatTuningTests
    {
        private static EffectNode Cast(EffectNode step) => new Trigger(TriggerKind.OnCast, step);
        private static Skill Of(params SkillPrimitive[] primitives) => new("Test Skill", primitives);

        [Fact]
        public void Default_MatchesTheSeededConstants()
        {
            // Projectile tier 2 → magnitude 3 → 3 × ProjectileDamage(10) = 30.
            var graph = Cast(new Emit(EmitDelivery.Projectile, 2));
            Assert.Equal(30f, GraphSkillResolver.Resolve(graph, 1).Hits[0].Damage, precision: 3);
        }

        [Fact]
        public void CustomTuning_ScalesProjectileDamage()
        {
            var graph = Cast(new Emit(EmitDelivery.Projectile, 2)); // magnitude 3
            var buffed = CombatTuning.Default with { ProjectileDamage = 20f };

            Assert.Equal(60f, GraphSkillResolver.Resolve(graph, 1, buffed).Hits[0].Damage, precision: 3); // 3 × 20
        }

        [Fact]
        public void CustomTuning_ScalesFocusCost()
        {
            var skill = Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 2)); // 2 points
            var pricey = CombatTuning.Default with { FocusCostPerPoint = 10f };

            Assert.Equal(8f, FocusCost.Of(skill), precision: 3);          // 2 × 4 (default)
            Assert.Equal(20f, FocusCost.Of(skill, pricey), precision: 3); // 2 × 10
        }

        [Fact]
        public void CustomTuning_ScalesPassiveReduction()
        {
            // Shield tier 4 → magnitude 5 → 5 × PassiveShieldReduction.
            var graph = new Trigger(TriggerKind.Continuous, new Ward(WardEffect.Shield, 4));
            var tough = CombatTuning.Default with { PassiveShieldReduction = 0.1f };

            Assert.Equal(0.30f, GraphPassiveResolver.Resolve(graph).DamageReduction, precision: 3);        // 5 × 0.06
            Assert.Equal(0.50f, GraphPassiveResolver.Resolve(graph, tough).DamageReduction, precision: 3); // 5 × 0.10
        }
    }
}
