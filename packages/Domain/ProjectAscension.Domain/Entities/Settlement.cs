#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// The frontier outpost the player develops (settlement-evolution.md). The MVP's
    /// civilization-growth pillar: the player delivers resources (자원 납품) to mature
    /// infrastructure (인프라 성숙도), and accumulated maturity advances the settlement's
    /// stage. Server-persistent so the outpost grows across sessions — the first real
    /// player-driven world state. (Singleton for the slice; per-region later.)
    /// </summary>
    public class Settlement
    {
        public int Id { get; set; } // fixed singleton key (1) for the slice
        public string Name { get; set; } = string.Empty;

        // Delivered-resource progress per infrastructure track. Level = Points / PointsPerLevel.
        public int ShelterPoints { get; set; } // ← hide
        public int MarketPoints { get; set; }  // ← feather
        public int DefensePoints { get; set; } // ← core
    }
}
