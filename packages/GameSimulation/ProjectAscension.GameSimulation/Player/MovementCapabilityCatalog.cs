namespace ProjectAscension.GameSimulation.Player
{
    /// <summary>
    /// Movement capabilities the player has gained from discovered PASSIVES (e.g. extra air
    /// jumps → double jump), pushed in from the Game layer and read by the movement path.
    /// A static bridge across assembly boundaries, like CombatTuningCatalog — the movement
    /// simulation stays a pure function (it takes ExtraJumps via MovementSettings), and the
    /// client's PlayerData folds this value into those settings each tick.
    /// </summary>
    public static class MovementCapabilityCatalog
    {
        /// <summary>Air jumps beyond the ground jump (0 = single jump). Set by PassiveModifiers.</summary>
        public static int ExtraJumps { get; private set; }

        public static void Set(int extraJumps) => ExtraJumps = extraJumps < 0 ? 0 : extraJumps;
    }
}
