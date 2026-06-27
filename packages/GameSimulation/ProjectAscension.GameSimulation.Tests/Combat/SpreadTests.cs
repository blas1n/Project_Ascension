using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class SpreadTests
    {
        [Fact]
        public void Bloom_WidensTowardMax_AndCaps()
        {
            var spread = Spread.From(1f, 8f);
            spread = SpreadRules.Bloom(spread, 1.2f);
            Assert.Equal(2.2f, spread.Current, precision: 3);

            for (int i = 0; i < 20; i++) spread = SpreadRules.Bloom(spread, 1.2f);
            Assert.Equal(8f, spread.Current, precision: 3); // capped at max
        }

        [Fact]
        public void Recover_TightensTowardMin_AndFloors()
        {
            var spread = new Spread(8f, 1f, 8f);
            spread = SpreadRules.Recover(spread, 6f, 0.5f); // -3
            Assert.Equal(5f, spread.Current, precision: 3);

            spread = SpreadRules.Recover(spread, 6f, 10f); // floors at min
            Assert.Equal(1f, spread.Current, precision: 3);
        }

        [Fact]
        public void SustainedFire_ThenRest_BloomsThenRecovers()
        {
            var spread = Spread.From(1f, 8f);
            for (int i = 0; i < 3; i++) spread = SpreadRules.Bloom(spread, 1.2f); // 1 + 3.6 = 4.6
            Assert.Equal(4.6f, spread.Current, precision: 3);

            spread = SpreadRules.Recover(spread, 6f, 0.6f); // -3.6 -> 1.0
            Assert.Equal(1f, spread.Current, precision: 3);
        }
    }
}
