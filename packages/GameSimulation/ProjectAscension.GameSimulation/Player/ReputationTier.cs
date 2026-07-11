namespace ProjectAscension.GameSimulation.Player
{
    /// <summary>
    /// Stratifies the player's standing (명성) into a tier that gates NPC reactions (ADR: Unity is a
    /// shell). A pure rule so the tier boundaries are tested/tunable without Unity; the client only
    /// renders the reaction text for the returned tier.
    /// </summary>
    public static class ReputationTier
    {
        public const int Tier1AtReputation = 10;
        public const int Tier2AtReputation = 30;

        /// <summary>0 (newcomer), 1 (known), or 2 (renowned).</summary>
        public static int Of(int reputation)
            => reputation >= Tier2AtReputation ? 2 : reputation >= Tier1AtReputation ? 1 : 0;
    }
}
