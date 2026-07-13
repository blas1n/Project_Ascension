using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>
    /// Active blocking, headless. The properties that make a shield a DECISION rather than a stat:
    /// it does nothing unless raised, and it does nothing if you're hit from outside its front arc.
    /// </summary>
    public class BlockRulesTests
    {
        private const float Front = 1f;   // dead ahead
        private const float Side = 0f;    // straight to the flank
        private const float Behind = -1f; // from the back

        [Fact]
        public void AShieldThatIsNotRaised_DoesNothing()
        {
            // The whole point: no passive protection. Holding a shield is not defending with it.
            Assert.Equal(20f, BlockRules.Blocked(20f, isBlocking: false, facingDot: Front), precision: 3);
            Assert.False(BlockRules.Blocks(isBlocking: false, facingDot: Front, frontArcDot: 0.35f));
        }

        [Fact]
        public void ARaisedShield_AbsorbsAFrontalBlow()
        {
            // Default BlockReduction 0.75 → a quarter of the blow lands.
            Assert.Equal(5f, BlockRules.Blocked(20f, isBlocking: true, facingDot: Front), precision: 3);
            Assert.True(BlockRules.Blocks(isBlocking: true, facingDot: Front, frontArcDot: 0.35f));
        }

        [Theory]
        [InlineData(Side)]
        [InlineData(Behind)]
        public void ARaisedShield_DoesNotCoverTheFlankOrTheBack(float facingDot)
        {
            // Being flanked bypasses the shield entirely — positioning is part of defending.
            Assert.Equal(20f, BlockRules.Blocked(20f, isBlocking: true, facingDot), precision: 3);
            Assert.False(BlockRules.Blocks(isBlocking: true, facingDot, frontArcDot: 0.35f));
        }

        [Fact]
        public void TheFrontArc_IsTheBoundary()
        {
            const float arc = 0.35f;
            Assert.True(BlockRules.Blocks(true, facingDot: arc, frontArcDot: arc));          // exactly on the edge holds
            Assert.False(BlockRules.Blocks(true, facingDot: arc - 0.01f, frontArcDot: arc)); // just outside does not
        }

        [Fact]
        public void BlockStrength_IsDbDriven()
        {
            // A balance edit retunes every shield with no code change (CombatTuning is DB-backed).
            var flimsy = CombatTuning.Default with { BlockReduction = 0.25f };
            var tower = CombatTuning.Default with { BlockReduction = 1f };

            Assert.Equal(15f, BlockRules.Blocked(20f, true, Front, flimsy), precision: 3);
            Assert.Equal(0f, BlockRules.Blocked(20f, true, Front, tower), precision: 3); // fully absorbed
        }

        [Fact]
        public void AWiderArc_CoversMoreOfTheFlank()
        {
            var wide = CombatTuning.Default with { BlockFrontArcDot = -0.5f }; // nearly all-round
            Assert.True(BlockRules.Blocks(true, facingDot: Side, frontArcDot: wide.BlockFrontArcDot));
        }
    }
}
