using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class StatusTests
    {
        [Fact]
        public void Stun_SetsAndExpires()
        {
            var s = StatusRules.Apply(StatusState.None, ControlKind.Stun, 1.2f, strength: 0f);
            Assert.True(s.IsStunned);

            s = StatusRules.Tick(s, 1.5f);
            Assert.False(s.IsStunned);
        }

        [Fact]
        public void Slow_AppliesSkillDefinedSpeedMultiplier()
        {
            // The slow amount comes from the skill (its strength), not a fixed receiver value.
            var s = StatusRules.Apply(StatusState.None, ControlKind.Slow, 1f, strength: 0.4f);
            Assert.Equal(0.6f, s.SpeedMultiplier, precision: 3); // 1 - 0.4

            s = StatusRules.Tick(s, 1f);
            Assert.Equal(1f, s.SpeedMultiplier, precision: 3); // recovered
        }

        [Fact]
        public void Slow_StrongerWins()
        {
            var s = StatusRules.Apply(StatusState.None, ControlKind.Slow, 1f, strength: 0.3f);
            s = StatusRules.Apply(s, ControlKind.Slow, 1f, strength: 0.7f); // stronger
            Assert.Equal(0.3f, s.SpeedMultiplier, precision: 3); // 1 - 0.7
        }

        [Fact]
        public void Slow_IsCappedSoItCannotFullyStop()
        {
            var s = StatusRules.Apply(StatusState.None, ControlKind.Slow, 1f, strength: 5f); // absurd
            Assert.Equal(0.1f, s.SpeedMultiplier, precision: 3); // capped at 90% slow
        }

        [Fact]
        public void Apply_TakesTheLongerDuration()
        {
            var s = StatusRules.Apply(StatusState.None, ControlKind.Stun, 1f, strength: 0f);
            s = StatusRules.Apply(s, ControlKind.Stun, 0.5f, strength: 0f); // shorter, ignored
            Assert.Equal(1f, s.StunRemaining, precision: 3);
            s = StatusRules.Apply(s, ControlKind.Stun, 2f, strength: 0f);   // longer, extends
            Assert.Equal(2f, s.StunRemaining, precision: 3);
        }

        [Fact]
        public void Knockback_IsNotTrackedAsDuration()
        {
            var s = StatusRules.Apply(StatusState.None, ControlKind.Knockback, 1f, strength: 8f);
            Assert.Equal(StatusState.None, s);
        }
    }
}
