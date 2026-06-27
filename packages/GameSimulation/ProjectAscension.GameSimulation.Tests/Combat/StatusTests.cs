using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class StatusTests
    {
        [Fact]
        public void Stun_SetsAndExpires()
        {
            var s = StatusRules.Apply(StatusState.None, ControlKind.Stun, 1.2f);
            Assert.True(s.IsStunned);

            s = StatusRules.Tick(s, 1.5f);
            Assert.False(s.IsStunned);
        }

        [Fact]
        public void Slow_AppliesSpeedMultiplier()
        {
            var s = StatusRules.Apply(StatusState.None, ControlKind.Slow, 1f);
            Assert.Equal(0.5f, s.SpeedMultiplier(0.5f), precision: 3);

            s = StatusRules.Tick(s, 1f);
            Assert.Equal(1f, s.SpeedMultiplier(0.5f), precision: 3); // recovered
        }

        [Fact]
        public void Apply_TakesTheLongerDuration()
        {
            var s = StatusRules.Apply(StatusState.None, ControlKind.Stun, 1f);
            s = StatusRules.Apply(s, ControlKind.Stun, 0.5f); // shorter, ignored
            Assert.Equal(1f, s.StunRemaining, precision: 3);
            s = StatusRules.Apply(s, ControlKind.Stun, 2f);   // longer, extends
            Assert.Equal(2f, s.StunRemaining, precision: 3);
        }

        [Fact]
        public void Knockback_IsNotTrackedAsDuration()
        {
            var s = StatusRules.Apply(StatusState.None, ControlKind.Knockback, 1f);
            Assert.Equal(StatusState.None, s);
        }
    }
}
