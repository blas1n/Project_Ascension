namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Whether the player may rebind a discovered command to a hotkey RIGHT NOW.
    ///
    /// Binding a technique to a key is knowledge, not equipment — you carry your knowledge
    /// everywhere, so a command can be bound from the journal in the city OR the frontier
    /// (unlike a weapon, which is a physical object and stays at the Equipment Station). The one
    /// thing binding must not do is let a player re-sort their kit while a monster is actively
    /// working them over, so it locks for a short window after the player's own last combat
    /// activity — taking damage, or dealing it. The window is DB-driven
    /// (<see cref="CombatTuning.BindingCombatLockSeconds"/>), not a magic constant, and pure/
    /// headless-tested (ADR: Unity is a shell) — the shell only supplies the clock.
    /// </summary>
    public static class BindingRules
    {
        /// <summary>True when it is safe to rebind: either combat has never happened
        /// (<paramref name="lastCombatTime"/> is null), or enough time has passed since it last
        /// did.</summary>
        public static bool CanRebind(float? lastCombatTime, float time, float lockSeconds)
            => lastCombatTime is null || time - lastCombatTime.Value >= lockSeconds;
    }
}
