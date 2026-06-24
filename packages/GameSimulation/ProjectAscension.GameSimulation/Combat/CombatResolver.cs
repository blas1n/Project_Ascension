namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Deterministic combat outcomes. Unity handles spatial hit detection
    /// (raycast/overlap) and reports hits; the actual damage/health/death result
    /// is resolved here so it is authoritative and testable.
    /// </summary>
    public static class CombatResolver
    {
        public static Health ApplyDamage(Health health, float amount)
        {
            if (amount <= 0f) return health;
            var next = health.Current - amount;
            return health with { Current = next < 0f ? 0f : next };
        }
    }
}
