using System;
using ProjectAscension.GameSimulation.Monsters;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Monsters
{
    /// <summary>
    /// Monster spawn placement, headless (ADR: Unity is a shell) — WHERE a monster appears is a game
    /// fact, so it must be seeded and reproducible, not UnityEngine.Random.insideUnitCircle.
    /// </summary>
    public class SpawnPlacementTests
    {
        [Fact]
        public void SameSeed_ProducesTheSameRing()
        {
            var a = SpawnPlacement.Ring(seed: 777, count: 6, minDistance: 4f, radius: 18f);
            var b = SpawnPlacement.Ring(seed: 777, count: 6, minDistance: 4f, radius: 18f);

            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
                Assert.Equal(a[i], b[i]);
        }

        [Fact]
        public void EveryPoint_LiesWithinTheAnnulus()
        {
            const float min = 8f, max = 18f;
            var points = SpawnPlacement.Ring(seed: 1234, count: 50, minDistance: min, radius: max);

            foreach (var (x, z) in points)
            {
                float dist = MathF.Sqrt(x * x + z * z);
                Assert.InRange(dist, min - 1e-3f, max + 1e-3f);
            }
        }

        [Fact]
        public void NoTwoPoints_Coincide_ForAReasonableCount()
        {
            var points = SpawnPlacement.Ring(seed: 99, count: 30, minDistance: 5f, radius: 25f);

            for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                    Assert.NotEqual(points[i], points[j]);
        }

        [Fact]
        public void DifferentSeeds_ProduceDifferentRings()
        {
            var a = SpawnPlacement.Ring(seed: 1, count: 5, minDistance: 4f, radius: 18f);
            var b = SpawnPlacement.Ring(seed: 2, count: 5, minDistance: 4f, radius: 18f);

            Assert.NotEqual(a[0], b[0]);
        }

        [Fact]
        public void ZeroCount_ReturnsEmpty()
        {
            var points = SpawnPlacement.Ring(seed: 1, count: 0, minDistance: 4f, radius: 18f);
            Assert.Empty(points);
        }
    }
}
