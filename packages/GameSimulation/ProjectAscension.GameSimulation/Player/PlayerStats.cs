namespace ProjectAscension.GameSimulation.Player
{
    /// <summary>The player's DB-driven balance stats — health, movement, dodge, focus.
    /// Lives in the shared simulation so every client layer (movement in Player, health in
    /// Combat, focus in Game) can read the same fetched values.</summary>
    // A record class (not record struct) so it compiles under Unity's C# 9.
    public sealed record PlayerStats(
        float MaxHealth,
        float MoveSpeed,
        float JumpVelocity,
        float Gravity,
        float DodgeSpeed,
        float DodgeDuration,
        float MaxFocus,
        float FocusRegenPerSecond);

    /// <summary>Process-wide holder for the DB-driven player stats. The Game layer fills it
    /// from the server at startup; consumers read <see cref="Current"/>, falling back to
    /// their own authored defaults while it is null (offline / before the fetch).</summary>
    public static class PlayerStatsCatalog
    {
        public static PlayerStats Current { get; private set; }

        public static void Set(PlayerStats stats) => Current = stats;
    }
}
