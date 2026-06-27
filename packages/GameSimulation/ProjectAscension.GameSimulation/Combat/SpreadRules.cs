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
    }
}
