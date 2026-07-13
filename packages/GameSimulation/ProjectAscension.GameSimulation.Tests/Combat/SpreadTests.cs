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

        // The cone SAMPLE is the shot's actual hit direction (ADR: Unity is a shell) — it must be
        // deterministic, reproducible by anyone resolving the same shot, and confined to the cone.

        [Fact]
        public void Deviation_SameSeed_IsBitForBitIdentical()
        {
            var a = SpreadRules.Deviation(5f, 42);
            var b = SpreadRules.Deviation(5f, 42);
            Assert.Equal(a, b);
        }

        [Fact]
        public void Deviation_DifferentSeeds_DifferAtLeastOnce()
        {
            bool anyDifferent = false;
            var prev = SpreadRules.Deviation(5f, 0);
            for (uint seed = 1; seed < 20; seed++)
            {
                var next = SpreadRules.Deviation(5f, seed);
                if (next != prev) anyDifferent = true;
                prev = next;
            }
            Assert.True(anyDifferent);
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(1u)]
        [InlineData(123456u)]
        [InlineData(uint.MaxValue)]
        public void Deviation_AlwaysLiesWithinTheCone(uint seed)
        {
            const float angle = 6f;
            var (yaw, pitch) = SpreadRules.Deviation(angle, seed);
            float radius = System.MathF.Sqrt(yaw * yaw + pitch * pitch);
            Assert.True(radius <= angle + 1e-4f, $"radius {radius} exceeded cone angle {angle}");
        }

        [Fact]
        public void Deviation_ZeroAngle_YieldsZeroDeviation()
        {
            var (yaw, pitch) = SpreadRules.Deviation(0f, 999);
            Assert.Equal(0f, yaw, precision: 5);
            Assert.Equal(0f, pitch, precision: 5);
        }
    }
}
