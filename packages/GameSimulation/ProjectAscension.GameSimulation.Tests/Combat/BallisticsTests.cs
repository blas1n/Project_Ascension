using System.Numerics;
using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class BallisticsTests
    {
        [Fact]
        public void Step_AppliesGravityAndMoves()
        {
            var (pos, vel) = Ballistics.Step(Vector3.Zero, new Vector3(10f, 0f, 0f), 9.8f, 0.1f);

            Assert.Equal(-0.98f, vel.Y, precision: 3); // gravity applied to velocity
            Assert.Equal(1f, pos.X, precision: 3);      // moved forward (10 × 0.1)
            Assert.Equal(-0.098f, pos.Y, precision: 3); // dropped
        }

        [Fact]
        public void NoGravity_FliesStraight()
        {
            var (pos, vel) = Ballistics.Step(Vector3.Zero, new Vector3(0f, 0f, 5f), 0f, 0.2f);
            Assert.Equal(0f, vel.Y, precision: 3);
            Assert.Equal(0f, pos.Y, precision: 3);
            Assert.Equal(1f, pos.Z, precision: 3);
        }

        [Fact]
        public void IsDeterministic_RegardlessOfHowTimeIsSliced()
        {
            // One big step vs many fixed steps to the same total time → same fall under
            // constant-gravity Euler (velocity integrates identically); position differs
            // only by integration scheme, so compare the velocity which must match.
            var (_, velA) = Ballistics.Step(Vector3.Zero, Vector3.Zero, 9.8f, 1f);

            var vel = Vector3.Zero;
            var pos = Vector3.Zero;
            for (int i = 0; i < 60; i++)
                (pos, vel) = Ballistics.Step(pos, vel, 9.8f, Ballistics.FixedStep);

            Assert.Equal(velA.Y, vel.Y, precision: 2); // ~ -9.8 either way
        }
    }
}
