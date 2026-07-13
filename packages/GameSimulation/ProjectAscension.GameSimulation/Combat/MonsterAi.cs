namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>How a monster is behaving. The deterministic state a monster AI moves through —
    /// the logic lives here (headless, testable), not in the Unity MonoBehaviour, which only reads
    /// the result to move/attack/render. <see cref="Winding"/> is the telegraph: the monster has
    /// committed to a strike and is winding up, giving the player a window to read it and move.</summary>
    public enum MonsterState { Idle, Chase, Winding, Attack, Dead }

    /// <summary>A monster's tuned AI ranges/timing (from MonsterStats — DB-driven). <see cref="AttackWindup"/>
    /// is the telegraph duration before a strike lands; 0 = strike instantly (no tell).</summary>
    public sealed record MonsterAiSettings(
        float MoveSpeed, float AggroRange, float AttackRange, float AttackCooldown, float AttackWindup = 0f)
    {
        public const float KnockbackDecay = 30f; // how fast a monster recovers from a push (was hard-coded in MonsterBase)
    }

    /// <summary>The outcome of one AI step: the next state, and whether — THIS tick — the monster
    /// should move toward the target, begin/continue telegraphing a strike, and/or land its attack.
    /// The MonoBehaviour applies these (MoveTowards / play the tell / PerformAttack) and renders; it
    /// makes no decisions. <see cref="WindupEndTime"/> is carried back in on the next tick so the
    /// wind-up timing persists across frames without state in the shell.</summary>
    public readonly struct MonsterAiResult
    {
        public readonly MonsterState State;
        public readonly bool Move;
        public readonly bool Attack;
        public readonly bool Telegraph;
        public readonly float NextAttackTime;
        public readonly float WindupEndTime;

        public MonsterAiResult(MonsterState state, bool move, bool attack, bool telegraph,
            float nextAttackTime, float windupEndTime)
        {
            State = state;
            Move = move;
            Attack = attack;
            Telegraph = telegraph;
            NextAttackTime = nextAttackTime;
            WindupEndTime = windupEndTime;
        }
    }

    /// <summary>
    /// The deterministic monster AI state machine (ADR: Unity is a shell). Given the current state,
    /// the distance to the target, whether it can act, and the clock, it returns the next state and
    /// this tick's actions. Idle → Chase (in aggro range) → Attack (in attack range) → Winding (on
    /// cooldown, telegraphing) → strike → Attack. A target that steps out of range during the wind-up
    /// makes the strike whiff (the reward for reading the tell); a stun interrupts it. Stunned or
    /// target-less monsters hold. Pure and headless-testable so behaviour/balance is verified without
    /// Unity. With <see cref="MonsterAiSettings.AttackWindup"/> = 0 it collapses to the old
    /// instant-strike behaviour (telegraph is additive).
    /// </summary>
    public static class MonsterAi
    {
        public static MonsterAiResult Step(
            MonsterState state, MonsterAiSettings settings,
            float distanceToTarget, bool hasTarget, bool isStunned,
            float time, float nextAttackTime, float windupEndTime = 0f)
        {
            // Dead or target-less: no state change, no actions (knockback still pushes it, but that's
            // physics the caller applies separately).
            if (state == MonsterState.Dead || !hasTarget)
                return new MonsterAiResult(state, false, false, false, nextAttackTime, windupEndTime);

            // Stunned interrupts a wind-up — a landed stun cancels the telegraphed strike (a real
            // reason to control a winding monster) — and otherwise holds.
            if (isStunned)
            {
                var held = state == MonsterState.Winding ? MonsterState.Attack : state;
                return new MonsterAiResult(held, false, false, false, nextAttackTime, windupEndTime);
            }

            bool move = false, attack = false, telegraph = false;
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
                    {
                        state = MonsterState.Chase;
                    }
                    else if (time >= nextAttackTime)
                    {
                        if (settings.AttackWindup > 0f)
                        {
                            // Commit to a strike and telegraph it — the player can read it and move now.
                            state = MonsterState.Winding;
                            windupEndTime = time + settings.AttackWindup;
                            telegraph = true;
                        }
                        else
                        {
                            // No wind-up configured → strike instantly (legacy behaviour).
                            attack = true;
                            nextAttackTime = time + settings.AttackCooldown;
                        }
                    }
                    break;

                case MonsterState.Winding:
                    if (distanceToTarget > settings.AttackRange)
                    {
                        // The target escaped the telegraph — the strike whiffs (reading the tell paid off).
                        state = MonsterState.Chase;
                    }
                    else if (time >= windupEndTime)
                    {
                        // Wind-up complete: the strike lands, and the cooldown starts now.
                        attack = true;
                        nextAttackTime = time + settings.AttackCooldown;
                        state = MonsterState.Attack;
                    }
                    else
                    {
                        telegraph = true; // still winding
                    }
                    break;
            }

            return new MonsterAiResult(state, move, attack, telegraph, nextAttackTime, windupEndTime);
        }
    }
}
