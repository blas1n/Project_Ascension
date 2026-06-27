using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class FocusTests
    {
        [Fact]
        public void Spend_WhenAffordable_Deducts()
        {
            Assert.True(FocusRules.TrySpend(Focus.Full(100f), 30f, out var result));
            Assert.Equal(70f, result.Current, precision: 3);
        }

        [Fact]
        public void Spend_WhenTooExpensive_Fails_AndLeavesUnchanged()
        {
            var focus = new Focus(20f, 100f);
            Assert.False(FocusRules.TrySpend(focus, 30f, out var result));
            Assert.Equal(20f, result.Current, precision: 3);
        }

        [Fact]
        public void Regenerate_CapsAtMax()
        {
            var focus = FocusRules.Regenerate(new Focus(90f, 100f), 25f);
            Assert.Equal(100f, focus.Current, precision: 3);
        }

        [Fact]
        public void Cost_ScalesWithPrimitives()
        {
            // (2 + 1 + 0) + (1 + 0 + 0) = 4 points × 4 = 16.
            var skill = new Skill("Bolt", new[]
            {
                new SkillPrimitive(SkillPrimitiveKind.Projectile, 2, Range: 1),
                new SkillPrimitive(SkillPrimitiveKind.Fork, 1),
            });
            Assert.Equal(16f, FocusCost.Of(skill), precision: 3);
        }
    }
}
