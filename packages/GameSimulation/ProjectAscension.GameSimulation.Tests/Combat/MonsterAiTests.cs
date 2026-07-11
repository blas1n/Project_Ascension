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

        // --- Telegraph / wind-up (AttackWindup > 0): a strike is announced before it lands. ---

        private static readonly MonsterAiSettings SW =
            new(MoveSpeed: 3f, AggroRange: 20f, AttackRange: 2f, AttackCooldown: 1f, AttackWindup: 0.4f);

        [Fact]
        public void Attack_WithWindup_TelegraphsBeforeStriking()
        {
            // On cooldown, the monster commits to a strike: it winds up (telegraph) instead of
            // dealing damage on the same tick, and records when the strike will land.
            var wind = MonsterAi.Step(MonsterState.Attack, SW, distanceToTarget: 1.5f,
                hasTarget: true, isStunned: false, time: 5f, nextAttackTime: 0f, windupEndTime: 0f);
            Assert.Equal(MonsterState.Winding, wind.State);
            Assert.True(wind.Telegraph);
            Assert.False(wind.Attack);                 // no damage yet — this is the tell
            Assert.Equal(5.4f, wind.WindupEndTime, precision: 3);
        }

        [Fact]
        public void Winding_StrikesWhenTheWindupCompletes()
        {
            // Mid wind-up: still telegraphing, no strike.
            var mid = MonsterAi.Step(MonsterState.Winding, SW, distanceToTarget: 1.5f,
                hasTarget: true, isStunned: false, time: 5.2f, nextAttackTime: 0f, windupEndTime: 5.4f);
            Assert.Equal(MonsterState.Winding, mid.State);
            Assert.True(mid.Telegraph);
            Assert.False(mid.Attack);

            // Wind-up complete: the strike lands and the cooldown starts now.
            var hit = MonsterAi.Step(MonsterState.Winding, SW, distanceToTarget: 1.5f,
                hasTarget: true, isStunned: false, time: 5.4f, nextAttackTime: 0f, windupEndTime: 5.4f);
            Assert.True(hit.Attack);
            Assert.Equal(MonsterState.Attack, hit.State);
            Assert.Equal(6.4f, hit.NextAttackTime, precision: 3); // 5.4 + cooldown
        }

        [Fact]
        public void Winding_WhiffsWhenTheTargetEscapesTheTell()
        {
            // Dodging out of range during the wind-up makes the strike miss — the payoff for
            // reading the telegraph.
            var whiff = MonsterAi.Step(MonsterState.Winding, SW, distanceToTarget: 5f,
                hasTarget: true, isStunned: false, time: 5.4f, nextAttackTime: 0f, windupEndTime: 5.4f);
            Assert.Equal(MonsterState.Chase, whiff.State);
            Assert.False(whiff.Attack);
        }

        [Fact]
        public void Winding_IsInterruptedByAStun()
        {
            // A stun landed during the wind-up cancels the telegraphed strike.
            var stunned = MonsterAi.Step(MonsterState.Winding, SW, distanceToTarget: 1.5f,
                hasTarget: true, isStunned: true, time: 5.3f, nextAttackTime: 0f, windupEndTime: 5.4f);
            Assert.False(stunned.Attack);
            Assert.False(stunned.Telegraph);
            Assert.Equal(MonsterState.Attack, stunned.State); // dropped out of the wind-up
        }
    }
}
