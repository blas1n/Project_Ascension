using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    // The combat numbers are no longer hard-coded: a CombatTuning (DB-driven in the host)
    // reshapes resolver output. Critical for the weapon-creation system — a balance edit
    // changes every discovered weapon's combat result without touching code.
    public class CombatTuningTests
    {
        private static Skill Of(params SkillPrimitive[] primitives) => new("Test Skill", primitives);

        [Fact]
        public void Default_MatchesTheSeededConstants()
        {
            var skill = Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 3));
            Assert.Equal(30f, SkillResolver.Resolve(skill, 1).Hits[0].Damage, precision: 3); // 3 × 10
        }

        [Fact]
        public void CustomTuning_ScalesProjectileDamage()
        {
            var skill = Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 3));
            var buffed = CombatTuning.Default with { ProjectileDamage = 20f };

            Assert.Equal(60f, SkillResolver.Resolve(skill, 1, buffed).Hits[0].Damage, precision: 3); // 3 × 20
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
            var skill = Of(new SkillPrimitive(SkillPrimitiveKind.Shield, 5));
            var tough = CombatTuning.Default with { PassiveShieldReduction = 0.1f };

            Assert.Equal(0.30f, PassiveResolver.Resolve(skill).DamageReduction, precision: 3);        // 5 × 0.06
            Assert.Equal(0.50f, PassiveResolver.Resolve(skill, tough).DamageReduction, precision: 3); // 5 × 0.10
        }
    }
}
