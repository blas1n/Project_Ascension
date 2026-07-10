namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Combat outcomes. The renderer/engine handles spatial hit detection
    /// (raycast/overlap) and reports hits; the actual damage/health/death result is
    /// resolved here so it is server-authoritative and testable (ADR 0004/0006 — the
    /// server resolves and replicates the result; the client renders it).
    /// </summary>
    public static class CombatResolver
    {
        public static Health ApplyDamage(Health health, float amount)
        {
            if (amount <= 0f) return health;
            var next = health.Current - amount;
            return health with { Current = next < 0f ? 0f : next };
        }

        /// <summary>Incoming damage after a defensive reduction fraction (0..1, clamped) — a
        /// passive's damage reduction. The rule lives here (tested, server-authoritative), not in
        /// the Unity HitReceiver.</summary>
        public static float Reduced(float amount, float reduction)
        {
            if (amount <= 0f) return 0f;
            float r = reduction < 0f ? 0f : reduction > 1f ? 1f : reduction;
            return amount * (1f - r);
        }

        /// <summary>Apply damage after a defensive reduction, in one atomic step.</summary>
        public static Health ApplyDamage(Health health, float amount, float reduction)
            => ApplyDamage(health, Reduced(amount, reduction));

        /// <summary>Restores health up to max (e.g. a skill's Leech self-heal). Does not
        /// revive the dead.</summary>
        public static Health ApplyHeal(Health health, float amount)
        {
            if (amount <= 0f || health.IsDead) return health;
            var next = health.Current + amount;
            return health with { Current = next > health.Max ? health.Max : next };
        }
    }
}
