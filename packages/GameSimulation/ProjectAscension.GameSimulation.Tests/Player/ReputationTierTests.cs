using ProjectAscension.GameSimulation.Player;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Player
{
    public class ReputationTierTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(9, 0)]
        [InlineData(10, 1)]  // tier-1 boundary
        [InlineData(29, 1)]
        [InlineData(30, 2)]  // tier-2 boundary
        [InlineData(120, 2)]
        public void Of_StratifiesAtTheBoundaries(int reputation, int expectedTier)
            => Assert.Equal(expectedTier, ReputationTier.Of(reputation));
    }
}
