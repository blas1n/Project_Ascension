using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>
    /// The deterministic PRNG that backs every gameplay-fact roll (bullet spread, spawn placement) —
    /// headless (ADR: Unity is a shell). The property that matters: same seed, same numbers, forever —
    /// that is the whole reason it exists instead of UnityEngine.Random.
    /// </summary>
    public class DeterministicRngTests
    {
        [Fact]
        public void SameSeed_ProducesTheSameStream_BitForBit()
        {
            var a = new DeterministicRng(12345);
            var b = new DeterministicRng(12345);

            for (int i = 0; i < 10; i++)
            {
                var (va, na) = a.NextUInt();
                var (vb, nb) = b.NextUInt();
                Assert.Equal(va, vb);
                a = na; b = nb;
            }
        }

        [Fact]
        public void DifferentSeeds_ProduceDifferentStreams()
        {
            var a = new DeterministicRng(1);
            var b = new DeterministicRng(2);

            var (va, _) = a.NextUInt();
            var (vb, _) = b.NextUInt();
            Assert.NotEqual(va, vb);
        }

        [Fact]
        public void NextFloat01_StaysInRange()
        {
            var rng = new DeterministicRng(999);
            for (int i = 0; i < 1000; i++)
            {
                var (v, next) = rng.NextFloat01();
                Assert.InRange(v, 0f, 1f);
                rng = next;
            }
        }

        [Fact]
        public void ZeroSeed_IsRemappedAwayFromTheFixedPoint()
        {
            // xorshift32 stalls at 0 forever; a seed of 0 must not silently produce an all-zero stream.
            var rng = new DeterministicRng(0);
            var (v, _) = rng.NextUInt();
            Assert.NotEqual(0u, v);
        }

        [Fact]
        public void Combine_IsDeterministic_AndOrderSensitive()
        {
            Assert.Equal(DeterministicRng.Combine(7, 3), DeterministicRng.Combine(7, 3));
            Assert.NotEqual(DeterministicRng.Combine(7, 3), DeterministicRng.Combine(3, 7));
            Assert.NotEqual(DeterministicRng.Combine(7, 3), DeterministicRng.Combine(7, 4));
        }
    }
}
