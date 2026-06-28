namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>Process-wide holder for the DB-driven <see cref="CombatTuning"/>. The Game
    /// layer fills it from the server at startup; any layer (skill resolution in Game, the
    /// charge threshold in Player) reads <see cref="Current"/>, which defaults to
    /// <see cref="CombatTuning.Default"/> until/unless fetched (offline keeps the defaults).</summary>
    public static class CombatTuningCatalog
    {
        public static CombatTuning Current { get; private set; } = CombatTuning.Default;

        public static void Set(CombatTuning tuning) => Current = tuning ?? CombatTuning.Default;
    }
}
