using System;
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Harness
{
    /// <summary>
    /// Deterministic, seeded generator of effect GRAPHS across the whole vocabulary (ADR 0007) —
    /// the input side of the headless runtime simulation. It stands in for the AI: instead of
    /// hand-authoring a few graphs, it produces thousands of shapes (every trigger × effect mix)
    /// so the runtime interpreters are exercised over combinations no manual playtest could reach.
    /// Same seed → same graph, so a failing case is reproducible.
    ///
    /// It intentionally makes ARBITRARY graphs — including ones the server validator would reject
    /// (over budget, odd mixes) — because the runtime must be robust to any graph the client is
    /// handed; the server owns validation, the client only executes.
    /// </summary>
    public static class GraphFuzzer
    {
        private static readonly TriggerKind[] Triggers =
            { TriggerKind.OnCast, TriggerKind.OnJumpInAir, TriggerKind.OnHit, TriggerKind.OnWallContact, TriggerKind.Continuous };
        private static readonly EmitDelivery[] Deliveries =
            { EmitDelivery.Projectile, EmitDelivery.Beam, EmitDelivery.Burst, EmitDelivery.Nova };
        private static readonly ImpulseDirection[] Directions =
            { ImpulseDirection.Up, ImpulseDirection.Forward, ImpulseDirection.Aim };
        private static readonly ControlEffect[] Controls =
            { ControlEffect.Knockback, ControlEffect.Slow, ControlEffect.Stun };
        private static readonly WardEffect[] Wards =
            { WardEffect.Shield, WardEffect.Barrier, WardEffect.Heal, WardEffect.Leech };

        /// <summary>A random full skill graph: a trigger over 1..maxSteps effect nodes.</summary>
        public static EffectNode Generate(Random rng, int maxSteps = 5)
        {
            var kind = Triggers[rng.Next(Triggers.Length)];
            int n = rng.Next(1, maxSteps + 1);
            if (n == 1) return new Trigger(kind, RandomEffect(rng));

            var steps = new List<EffectNode>(n);
            for (int i = 0; i < n; i++) steps.Add(RandomEffect(rng));
            return new Trigger(kind, new Sequence(steps));
        }

        /// <summary>A random OFFENSIVE graph (OnCast + at least one Emit) — for combat invariants.</summary>
        public static EffectNode GenerateOffensive(Random rng, int maxSteps = 5)
        {
            var steps = new List<EffectNode> { new Emit(Deliveries[rng.Next(Deliveries.Length)], Tier(rng)) };
            int extra = rng.Next(0, maxSteps);
            for (int i = 0; i < extra; i++) steps.Add(RandomEffect(rng));
            return new Trigger(TriggerKind.OnCast, steps.Count == 1 ? steps[0] : new Sequence(steps));
        }

        /// <summary>A random MOVEMENT graph (a movement trigger + an impulse) — for movement invariants.</summary>
        public static EffectNode GenerateMovement(Random rng)
        {
            var movementTriggers = new[] { TriggerKind.OnJumpInAir, TriggerKind.OnWallContact };
            var kind = movementTriggers[rng.Next(movementTriggers.Length)];
            return new Trigger(kind, new Impulse(Directions[rng.Next(Directions.Length)], Tier(rng)));
        }

        private static EffectNode RandomEffect(Random rng)
        {
            switch (rng.Next(8))
            {
                case 0: return new Emit(Deliveries[rng.Next(Deliveries.Length)], Tier(rng));
                case 1: return new Damage(Tier(rng));
                case 2: return new Dot(Tier(rng), rng.Next(0, 5));
                case 3: return new Spread(Tier(rng));
                case 4: return new Homing(Tier(rng));
                case 5: return new Control(Controls[rng.Next(Controls.Length)], Tier(rng));
                case 6: return new Ward(Wards[rng.Next(Wards.Length)], Tier(rng));
                default: return new Impulse(Directions[rng.Next(Directions.Length)], Tier(rng));
            }
        }

        private static int Tier(Random rng) => rng.Next(0, 4); // 0..3 (EffectGraph.MaxTier)
    }
}
