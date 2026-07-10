using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>
    /// The monster AI state machine, now headless (ADR: Unity is a shell) — so aggro/chase/attack
    /// transitions and the attack cooldown are testable/tunable without running Unity.
    /// </summary>
    public class MonsterAiTests
    {
        private static readonly MonsterAiSettings S = new(MoveSpeed: 3f, AggroRange: 20f, AttackRange: 2f, AttackCooldown: 1f);

        private static MonsterAiResult Step(MonsterState state, float dist, float time = 0f, float next = 0f,
            bool hasTarget = true, bool stunned = false)
            => MonsterAi.Step(state, S, dist, hasTarget, stunned, time, next);

        [Fact]
        public void Idle_AggrosWhenInRange_HoldsOtherwise()
        {
            Assert.Equal(MonsterState.Idle, Step(MonsterState.Idle, dist: 25f).State);   // out of aggro
            Assert.Equal(MonsterState.Chase, Step(MonsterState.Idle, dist: 15f).State);   // in aggro
        }

        [Fact]
        public void Chase_MovesUntilInAttackRange_ThenSwitchesToAttack()
        {
            var chasing = Step(MonsterState.Chase, dist: 10f);
            Assert.Equal(MonsterState.Chase, chasing.State);
            Assert.True(chasing.Move);
            Assert.False(chasing.Attack); // doesn't attack while chasing

            var arrived = Step(MonsterState.Chase, dist: 1.5f);
            Assert.Equal(MonsterState.Attack, arrived.State);
            Assert.False(arrived.Move);   // switches to attack, no move this tick
            Assert.False(arrived.Attack); // and doesn't fire on the transition tick
        }

        [Fact]
        public void Attack_FiresOnCooldown_ThenGatesUntilReady()
        {
            // First tick past the ready time: fires + sets the next ready time a cooldown ahead.
            var fired = Step(MonsterState.Attack, dist: 1.5f, time: 5f, next: 0f);
            Assert.True(fired.Attack);
            Assert.Equal(6f, fired.NextAttackTime, precision: 3);

            // Still within the cooldown: no attack.
            var onCd = Step(MonsterState.Attack, dist: 1.5f, time: 5.5f, next: 6f);
            Assert.False(onCd.Attack);
        }

        [Fact]
        public void Attack_ReturnsToChase_WhenTargetLeavesRange()
        {
            var left = Step(MonsterState.Attack, dist: 5f, time: 10f, next: 0f);
            Assert.Equal(MonsterState.Chase, left.State);
            Assert.False(left.Attack);
        }

        [Fact]
        public void StunnedOrTargetless_HoldsAndDoesNothing()
        {
            var stunned = Step(MonsterState.Attack, dist: 1.5f, time: 10f, next: 0f, stunned: true);
            Assert.Equal(MonsterState.Attack, stunned.State); // state frozen
            Assert.False(stunned.Move);
            Assert.False(stunned.Attack);

            var noTarget = Step(MonsterState.Chase, dist: float.MaxValue, hasTarget: false);
            Assert.False(noTarget.Move);
            Assert.False(noTarget.Attack);
        }

        [Fact]
        public void Dead_NeverActs()
        {
            var dead = Step(MonsterState.Dead, dist: 1f, time: 99f);
            Assert.Equal(MonsterState.Dead, dead.State);
            Assert.False(dead.Move);
            Assert.False(dead.Attack);
        }
    }
}
