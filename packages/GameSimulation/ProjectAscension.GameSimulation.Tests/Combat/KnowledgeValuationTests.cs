using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class KnowledgeValuationTests
    {
        private static Skill Of(params SkillPrimitive[] primitives) => new("Test", primitives);

        [Fact]
        public void PowerPoints_SumsMagnitudeRangeDuration()
        {
            var skill = Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 3, Range: 1),
                           new SkillPrimitive(SkillPrimitiveKind.DamageOverTime, 2, Duration: 2));
            Assert.Equal(8, KnowledgeValuation.PowerPoints(skill)); // (3+1) + (2+2)
        }

        [Fact]
        public void StrongerKnowledge_SellsForMore()
        {
            var weak = Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 1));
            var strong = Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 5));
            Assert.True(KnowledgeValuation.LicensePrice(strong, 6) > KnowledgeValuation.LicensePrice(weak, 6));
            Assert.Equal(30, KnowledgeValuation.LicensePrice(strong, 6)); // 5 × 6
        }

        [Fact]
        public void LicenseReputation_ScalesWithPower_AndZeroDivisorDisables()
        {
            var skill = Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 10));
            Assert.Equal(2, KnowledgeValuation.LicenseReputation(skill, 5)); // 10 / 5
            Assert.Equal(0, KnowledgeValuation.LicenseReputation(skill, 0));
        }
    }
}
