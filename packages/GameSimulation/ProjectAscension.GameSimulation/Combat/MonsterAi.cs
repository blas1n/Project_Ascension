namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>How a monster is behaving. The deterministic state a monster AI moves through —
    /// the logic lives here (headless, testable), not in the Unity MonoBehaviour, which only reads
    /// the result to move/attack/render.</summary>
    public enum MonsterState { Idle, Chase, Attack, Dead }

    /// <summary>A monster's tuned AI ranges/timing (from MonsterStats — DB-driven).</summary>
    public sealed record MonsterAiSettings(float MoveSpeed, float AggroRange, float AttackRange, float AttackCooldown)
    {
        public const float KnockbackDecay = 30f; // how fast a monster recovers from a push (was hard-coded in MonsterBase)
    }

    /// <summary>The outcome of one AI step: the next state, and whether — THIS tick — the monster
    /// should move toward the target and/or fire its attack. The MonoBehaviour applies these
    /// (MoveTowards / PerformAttack) and renders; it makes no decisions.</summary>
    public readonly struct MonsterAiResult
    {
        public readonly MonsterState State;
        public readonly bool Move;
        public readonly bool Attack;
        public readonly float NextAttackTime;

        public MonsterAiResult(MonsterState state, bool move, bool attack, float nextAttackTime)
        {
            State = state;
            Move = move;
            Attack = attack;
            NextAttackTime = nextAttackTime;
        }
    }

    /// <summary>
    /// The deterministic monster AI state machine (ADR: Unity is a shell). Given the current state,
    /// the distance to the target, whether it can act, and the clock, it returns the next state and
    /// this tick's actions. Idle → Chase (in aggro range) → Attack (in attack range, on cooldown);
    /// Attack → Chase if the target steps out of range. Stunned or target-less monsters hold. Pure
    /// and headless-testable so monster behaviour/balance is verified without Unity.
    /// </summary>
    public static class MonsterAi
    {
        public static MonsterAiResult Step(
            MonsterState state, MonsterAiSettings settings,
            float distanceToTarget, bool hasTarget, bool isStunned,
            float time, float nextAttackTime)
        {
            // Dead, target-less, or stunned: no state change, no move, no attack (knockback still
            // pushes it, but that's physics the caller applies separately).
            if (state == MonsterState.Dead || !hasTarget || isStunned)
                return new MonsterAiResult(state, false, false, nextAttackTime);

            bool move = false, attack = false;
            switch (state)
            {
                case MonsterState.Idle:
                    if (distanceToTarget <= settings.AggroRange) state = MonsterState.Chase;
                    break;

                case MonsterState.Chase:
                    if (distanceToTarget <= settings.AttackRange) state = MonsterState.Attack;
                    else move = true;
                    break;

                case MonsterState.Attack:
                    if (distanceToTarget > settings.AttackRange)
                        state = MonsterState.Chase;
                    else if (time >= nextAttackTime)
                    {
                        nextAttackTime = time + settings.AttackCooldown;
                        attack = true;
                    }
                    break;
            }

            return new MonsterAiResult(state, move, attack, nextAttackTime);
        }
    }
}
