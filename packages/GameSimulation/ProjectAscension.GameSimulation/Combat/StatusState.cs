using System;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// A target's timed control statuses — how much longer it is slowed and stunned.
    /// (Knockback is an instantaneous impulse, not a duration, so it is not tracked
    /// here.) A pure value so the timers are deterministic and testable; the renderer
    /// applies the consequences (reduced move speed, skipped actions).
    /// </summary>
    public record StatusState(float SlowRemaining, float StunRemaining, float SlowMultiplier)
    {
        public static readonly StatusState None = new(0f, 0f, 1f);

        public bool IsStunned => StunRemaining > 0f;
        public bool IsSlowed => SlowRemaining > 0f;

        /// <summary>Movement multiplier while slowed (the skill-defined factor), else 1.</summary>
        public float SpeedMultiplier => SlowRemaining > 0f ? SlowMultiplier : 1f;
    }

    /// <summary>Deterministic status transitions: apply a control for a duration and tick
    /// the timers down. Separate from <see cref="StatusState"/> so the state is a value
    /// and the rules are testable.</summary>
    public static class StatusRules
    {
        private const float MaxSlow = 0.9f; // a slow can't fully stop a target (min 10% speed)

        /// <summary>Apply a control. Slow/Stun take the longer of the current and the new
        /// duration; Slow's <paramref name="strength"/> (the skill's slow fraction) sets the
        /// move multiplier, the stronger slow winning. Knockback (an impulse) is elsewhere.</summary>
        public static StatusState Apply(StatusState state, ControlKind kind, float duration, float strength)
        {
            if (duration <= 0f) return state;
            switch (kind)
            {
                case ControlKind.Slow:
                    float mult = 1f - Math.Min(MaxSlow, Math.Max(0f, strength));
                    return state with
                    {
                        SlowRemaining = Math.Max(state.SlowRemaining, duration),
                        SlowMultiplier = state.IsSlowed ? Math.Min(state.SlowMultiplier, mult) : mult,
                    };
                case ControlKind.Stun:
                    return state with { StunRemaining = Math.Max(state.StunRemaining, duration) };
                default:
                    return state;
            }
        }

        public static StatusState Tick(StatusState state, float dt)
        {
            if (dt <= 0f) return state;
            return state with
            {
                SlowRemaining = state.SlowRemaining > dt ? state.SlowRemaining - dt : 0f,
                StunRemaining = state.StunRemaining > dt ? state.StunRemaining - dt : 0f,
            };
        }
    }
}
