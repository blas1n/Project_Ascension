using System;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Deterministic spread transitions: each shot blooms the cone toward the max;
    /// time without firing recovers it toward the min. Kept separate from
    /// <see cref="Spread"/> so the resource is a value and the rules are testable.
    /// </summary>
    public static class SpreadRules
    {
        /// <summary>Widen the cone by one shot's bloom, capped at max.</summary>
        public static Spread Bloom(Spread spread, float perShot)
        {
            if (perShot <= 0f) return spread;
            float next = spread.Current + perShot;
            return spread with { Current = next > spread.Max ? spread.Max : next };
        }

        /// <summary>Tighten the cone over <paramref name="dt"/> seconds, floored at min.</summary>
        public static Spread Recover(Spread spread, float ratePerSecond, float dt)
        {
            if (ratePerSecond <= 0f || dt <= 0f) return spread;
            float next = spread.Current - ratePerSecond * dt;
            return spread with { Current = next < spread.Min ? spread.Min : next };
        }

        /// <summary>
        /// Deterministically sample a point inside the spread cone: <paramref name="angleDegrees"/> is
        /// the cone's half-angle (the weapon's CURRENT spread), <paramref name="seed"/> is the per-shot
        /// fact that decides WHERE in the cone this particular shot lands. This is the shot's actual hit
        /// direction, not cosmetic — a server resolving the same shot from the same seed must get the
        /// same answer (ADR: Unity is a shell), so it cannot be sampled from an unseeded/engine RNG.
        ///
        /// Uniform over the DISK of radius <paramref name="angleDegrees"/> (area-uniform, not just
        /// angle-uniform — otherwise samples would bunch up at the centre) via the standard
        /// sqrt(u1)-radius / u2-angle disk transform.
        /// </summary>
        public static (float YawDegrees, float PitchDegrees) Deviation(float angleDegrees, uint seed)
        {
            if (angleDegrees <= 0f) return (0f, 0f);

            var rng = new DeterministicRng(seed);
            var (u1, next) = rng.NextFloat01();
            var (u2, _) = next.NextFloat01();

            float radius = angleDegrees * MathF.Sqrt(u1);
            float theta = u2 * MathF.PI * 2f;
            return (radius * MathF.Cos(theta), radius * MathF.Sin(theta));
        }
    }
}
