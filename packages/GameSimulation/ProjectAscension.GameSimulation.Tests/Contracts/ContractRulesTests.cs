using ProjectAscension.GameSimulation.Contracts;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Contracts
{
    /// <summary>
    /// The contract lifecycle rules, now headless (ADR: Unity is a shell) — progress clamping,
    /// timers, and accept/fail conditions are tested without Unity, so the core city↔contract loop
    /// is covered (the founding "a hunt on death must fail, a tutorial on death must not" cases).
    /// </summary>
    public class ContractRulesTests
    {
        [Fact]
        public void Progress_NeverExceedsTheTarget()
        {
            Assert.Equal(3, ContractRules.ClampedProgress(current: 2, amount: 1, target: 5) is var p && p == 3 ? 3 : p);
            Assert.Equal(5, ContractRules.ClampedProgress(current: 4, amount: 10, target: 5)); // clamped
            Assert.True(ContractRules.IsComplete(5, 5));
            Assert.False(ContractRules.IsComplete(4, 5));
        }

        [Fact]
        public void CanAccept_GatesOnReputation()
        {
            Assert.True(ContractRules.CanAccept(reputation: 30, minReputation: 30));
            Assert.True(ContractRules.CanAccept(reputation: 50, minReputation: 30));
            Assert.False(ContractRules.CanAccept(reputation: 29, minReputation: 30));
        }

        [Fact]
        public void TickTimer_ElapsesAtOrBelowZero()
        {
            var (r1, e1) = ContractRules.TickTimer(2f, 0.5f);
            Assert.Equal(1.5f, r1, precision: 3);
            Assert.False(e1);

            var (_, e2) = ContractRules.TickTimer(0.3f, 0.5f);
            Assert.True(e2); // crossed zero → elapsed

            var (_, e3) = ContractRules.TickTimer(0.5f, 0.5f);
            Assert.True(e3); // exactly zero also elapses
        }

        [Fact]
        public void FailsOnDeath_OnlyWhenOptedIn_AndNotComplete()
        {
            Assert.True(ContractRules.FailsOnDeath(failOnDeath: true, isComplete: false));   // a hunt dies → fail
            Assert.False(ContractRules.FailsOnDeath(failOnDeath: false, isComplete: false)); // tutorial death → no fail
            Assert.False(ContractRules.FailsOnDeath(failOnDeath: true, isComplete: true));   // already done → no fail
        }

        [Fact]
        public void CanExpire_OnlyWhenOptedIn_AndNotComplete()
        {
            Assert.True(ContractRules.CanExpire(failOnTimeout: true, isComplete: false));
            Assert.False(ContractRules.CanExpire(failOnTimeout: false, isComplete: false));
            Assert.False(ContractRules.CanExpire(failOnTimeout: true, isComplete: true));
        }
    }
}
