using ProjectAscension.GameSimulation.Discovery;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Discovery
{
    /// <summary>
    /// The composite behaviours, headless. These carry the discovery promise — everyone jumps and
    /// attacks, so it is the SHAPE of play (out of a dodge, from the air, chained) that has to make
    /// one player's discovery differ from another's.
    /// </summary>
    public class BehaviorDeriverTests
    {
        [Fact]
        public void AnAttackRightAfterADodge_IsADodgeAttack()
        {
            // Times from 0 so the boundary is exact in float — the rule is `<= window`, and a test
            // shouldn't fail on 10.6f - 10f drifting a few ulps past 0.6f.
            var d = new BehaviorDeriver(dodgeAttackWindow: 0.6f);
            d.Dodged(0f);

            Assert.True(d.IsDodgeAttack(0.1f)); // straight out of the roll
            Assert.True(d.IsDodgeAttack(0.6f)); // the edge of the window still counts
        }

        [Fact]
        public void AnAttackLongAfterADodge_IsJustAnAttack()
        {
            var d = new BehaviorDeriver(dodgeAttackWindow: 0.6f);
            d.Dodged(0f);
            Assert.False(d.IsDodgeAttack(0.7f));
        }

        [Fact]
        public void AttackingWithoutEverDodging_IsNeverADodgeAttack()
        {
            // No dodge has happened, so nothing may look like one (guards the -infinity seed).
            var d = new BehaviorDeriver();
            Assert.False(d.IsDodgeAttack(0f));
            Assert.False(d.IsDodgeAttack(9999f));
        }

        [Fact]
        public void ChainedJumps_ReadAsRepeatedJumping_OnlyOnceTheChainIsLongEnough()
        {
            var d = new BehaviorDeriver(jumpChainWindow: 1.2f, repeatedJumpCount: 3);

            Assert.False(d.Jumped(0f));   // 1 — just a jump
            Assert.False(d.Jumped(0.5f)); // 2 — still just jumping
            Assert.True(d.Jumped(1.0f));  // 3 — now it's deliberate
            Assert.True(d.Jumped(1.5f));  // and it stays true while they keep bouncing
            Assert.Equal(4, d.JumpStreak);
        }

        [Fact]
        public void APauseBreaksTheChain()
        {
            var d = new BehaviorDeriver(jumpChainWindow: 1.2f, repeatedJumpCount: 3);
            d.Jumped(0f);
            d.Jumped(0.5f);

            Assert.False(d.Jumped(5f));   // too long a gap — this is a fresh jump, not a chain
            Assert.Equal(1, d.JumpStreak);
            Assert.False(d.Jumped(5.4f));
            Assert.True(d.Jumped(5.8f));  // chained again from scratch
        }

        [Fact]
        public void ASingleJump_IsNeverRepeated()
        {
            var d = new BehaviorDeriver(repeatedJumpCount: 3);
            Assert.False(d.Jumped(1f));
            Assert.Equal(1, d.JumpStreak);
        }
    }
}
