#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// Single-row tuning for economy transactions the server settles directly (as opposed to
    /// combat, which the client also predicts). Currently just the knowledge-license rate —
    /// gold and standing (명성) a discovered skill's power converts to when its license is sold.
    /// Runtime-editable (balance numbers are DB-driven, never hard-coded — CLAUDE.md).
    /// </summary>
    public class EconomyTuning
    {
        public int Id { get; set; } // fixed singleton key (1)

        public int KnowledgeGoldPerPoint { get; set; }  // knowledge license price per power point
        public int KnowledgePointsPerRep { get; set; }   // power per standing point from a license sale
    }
}
