using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class ChargeRulesTests
    {
        [Fact]
        public void NoCharge_IsBaseMultiplier()
            => Assert.Equal(1f, ChargeRules.Multiplier(0f, 2.5f), precision: 3);

        [Fact]
        public void FullCharge_IsMax()
            => Assert.Equal(2.5f, ChargeRules.Multiplier(1f, 2.5f), precision: 3);

        [Fact]
        public void HalfCharge_IsHalfway()
            => Assert.Equal(2f, ChargeRules.Multiplier(0.5f, 3f), precision: 3);

        [Theory]
        [InlineData(-1f, 1f)]   // clamps below
        [InlineData(2f, 2.5f)]  // clamps above to max
        public void Clamps(float charge, float expected)
            => Assert.Equal(expected, ChargeRules.Multiplier(charge, 2.5f), precision: 3);

        [Fact]
        public void MaxBelowOne_TreatedAsOne()
            => Assert.Equal(1f, ChargeRules.Multiplier(1f, 0f), precision: 3);
    }
}
