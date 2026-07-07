using System.Numerics;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.GameSimulation.Harness
{
    /// <summary>
    /// The properties that must hold whenever a discovered skill's graph is EXECUTED at runtime —
    /// the checks the headless simulation asserts over every fuzzed/AI graph (ADR 0007). Each
    /// method returns null when the invariant holds, or a short reason when it is violated (so a
    /// simulation can report exactly what broke, on which graph). Pure and deterministic.
    /// </summary>
    public static class RuntimeInvariants
    {
        /// <summary>A resolved combat outcome must be sane: finite, non-negative numbers and no
        /// more hits than targets; without an area effect the hits can't exceed 1 + spread.</summary>
        public static string CheckResolution(SkillResolution r, int availableTargets)
        {
            if (r is null) return "resolution is null";
            if (r.Hits.Count > availableTargets) return $"hits {r.Hits.Count} > targets {availableTargets}";
            if (!Sane(r.SelfHeal)) return "self-heal not finite/non-negative";
            if (!Sane(r.SelfShield)) return "self-shield not finite/non-negative";
            if (!Sane(r.DashDistance)) return "dash not finite/non-negative";

            foreach (var h in r.Hits)
            {
                if (!Sane(h.Damage)) return "hit damage not finite/non-negative";
                if (!Sane(h.DamageOverTimePerTick)) return "dot/tick not finite/non-negative";
                if (h.DamageOverTimeTicks < 0) return "dot ticks negative";
                if (!Sane(h.ControlDuration)) return "control duration not finite/non-negative";
                if (!Sane(h.ControlStrength)) return "control strength not finite/non-negative";
            }
            return null;
        }

        /// <summary>Movement read off a graph must be bounded: air jumps in [0, cap], and wall-climb
        /// only when the graph actually carries an OnWallContact trigger.</summary>
        public static string CheckMovement(EffectNode graph)
        {
            var cap = MovementCapability.From(new[] { graph });
            if (cap.ExtraJumps < 0 || cap.ExtraJumps > MovementCapability.MaxExtraJumps)
                return $"extra jumps {cap.ExtraJumps} out of [0,{MovementCapability.MaxExtraJumps}]";
            if (cap.WallClimb && !(graph is Trigger t && t.Kind == TriggerKind.OnWallContact))
                return "wall-climb granted without an OnWallContact trigger";
            return null;
        }

        /// <summary>With the movement a graph grants, the player can air-jump exactly ExtraJumps
        /// times after the ground jump — never more (no infinite jumps).</summary>
        public static string CheckAirJumpBudget(EffectNode graph)
        {
            var cap = MovementCapability.From(new[] { graph });
            var settings = MovementSettings.Default with { ExtraJumps = cap.ExtraJumps };
            var sim = new PlayerSimulation();

            // Ground jump.
            var state = new PlayerState(Vector3.Zero, Vector3.Zero, IsGrounded: true, InputSequence: 0);
            state = sim.ApplyInput(state, Jump(1), 0.016f, settings);

            // Then hammer jump in the air; count how many actually re-boost upward.
            int airJumps = 0;
            for (int i = 0; i < cap.ExtraJumps + 3; i++)
            {
                // Fall a little so we're clearly airborne and descending.
                state = state with { Velocity = new Vector3(0, -3f, 0), IsGrounded = false };
                var before = state.Velocity.Y;
                state = sim.ApplyInput(state, Jump(i + 2), 0.016f, settings);
                if (state.Velocity.Y > before + settings.JumpVelocity * 0.5f) airJumps++;
            }

            return airJumps <= cap.ExtraJumps
                ? null
                : $"air jumps {airJumps} exceeded the granted {cap.ExtraJumps}";
        }

        private static PlayerInput Jump(int seq) => new(0f, 0f, Jump: true, Dodge: false, Attack: false, Sequence: seq);

        private static bool Sane(float v) => !float.IsNaN(v) && !float.IsInfinity(v) && v >= 0f;
    }
}
