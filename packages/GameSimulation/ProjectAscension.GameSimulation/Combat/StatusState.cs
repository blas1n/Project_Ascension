using System;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// A target's timed control statuses — how much longer it is slowed and stunned.
    /// (Knockback is an instantaneous impulse, not a duration, so it is not tracked
    /// here.) A pure value so the timers are deterministic and testable; the renderer
    /// applies the consequences (reduced move speed, skipped actions).
    /// </summary>
    public record StatusState(float SlowRemaining, float StunRemaining)
    {
        public static readonly StatusState None = new(0f, 0f);

        public bool IsStunned => StunRemaining > 0f;
        public bool IsSlowed => SlowRemaining > 0f;

        /// <summary>Movement multiplier — <paramref name="slowFactor"/> while slowed, else 1.</summary>
        public float SpeedMultiplier(float slowFactor) => SlowRemaining > 0f ? slowFactor : 1f;
    }

    /// <summary>Deterministic status transitions: apply a control for a duration and tick
    /// the timers down. Separate from <see cref="StatusState"/> so the state is a value
    /// and the rules are testable.</summary>
    public static class StatusRules
    {
        /// <summary>Apply a control. Slow/Stun set the remaining time to the longer of the
        /// current and the new duration; Knockback (an impulse) is handled elsewhere.</summary>
        public static StatusState Apply(StatusState state, ControlKind kind, float duration)
        {
            if (duration <= 0f) return state;
            return kind switch
            {
                ControlKind.Slow => state with { SlowRemaining = Math.Max(state.SlowRemaining, duration) },
                ControlKind.Stun => state with { StunRemaining = Math.Max(state.StunRemaining, duration) },
                _ => state,
            };
        }

        public static StatusState Tick(StatusState state, float dt)
        {
            if (dt <= 0f) return state;
            return new StatusState(
                state.SlowRemaining > dt ? state.SlowRemaining - dt : 0f,
                state.StunRemaining > dt ? state.StunRemaining - dt : 0f);
        }
    }
}
