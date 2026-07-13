namespace ProjectAscension.GameSimulation.Player
{
    /// <summary>
    /// Movement capabilities the player has gained from discovered skills' effect GRAPHS (ADR
    /// 0007 — e.g. extra air jumps, wall-climb), pushed in from the Game layer and read by the
    /// movement path. A static bridge across assembly boundaries, like CombatTuningCatalog — the
    /// movement simulation stays a pure function (it takes ExtraJumps via MovementSettings), and
    /// the client's PlayerData folds these values into those settings each tick.
    /// </summary>
    public static class MovementCapabilityCatalog
    {
        /// <summary>Air jumps beyond the ground jump (0 = single jump). From graph movement
        /// triggers (OnJumpInAir + upward impulse). Set by PassiveModifiers.</summary>
        public static int ExtraJumps { get; private set; }

        /// <summary>Whether a discovered skill grants wall-climb (a graph OnWallContact trigger).</summary>
        public static bool WallClimb { get; private set; }

        public static void Set(MovementCapability capability)
        {
            ExtraJumps = capability != null && capability.ExtraJumps > 0 ? capability.ExtraJumps : 0;
            WallClimb = capability != null && capability.WallClimb;
        }
    }
}
