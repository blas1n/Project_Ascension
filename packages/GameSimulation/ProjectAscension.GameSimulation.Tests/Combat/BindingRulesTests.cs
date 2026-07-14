using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>Binding is knowledge (bindable anywhere), but locked for a short DB-driven window
    /// after the player's own last combat activity — headless (ADR: Unity is a shell).</summary>
    public class BindingRulesTests
    {
        [Fact]
        public void NoCombatEver_CanAlwaysRebind()
            => Assert.True(BindingRules.CanRebind(lastCombatTime: null, time: 500f, lockSeconds: 3f));

        [Fact]
        public void RightAfterCombat_IsLocked()
            => Assert.False(BindingRules.CanRebind(lastCombatTime: 10f, time: 10.5f, lockSeconds: 3f));

        [Fact]
        public void OnceTheWindowElapses_IsFreeAgain()
            => Assert.True(BindingRules.CanRebind(lastCombatTime: 10f, time: 13f, lockSeconds: 3f));

        [Fact]
        public void ExactlyAtTheThreshold_IsFree()
            => Assert.True(BindingRules.CanRebind(lastCombatTime: 10f, time: 13f, lockSeconds: 3f)); // >= is free, not strictly >

        [Fact]
        public void JustBeforeTheThreshold_IsStillLocked()
            => Assert.False(BindingRules.CanRebind(lastCombatTime: 10f, time: 12.99f, lockSeconds: 3f));
    }
}
