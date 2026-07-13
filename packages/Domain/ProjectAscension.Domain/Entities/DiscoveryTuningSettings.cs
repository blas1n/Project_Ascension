#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// The single-row tunable coefficients for discovery scoring and power budget —
    /// server-managed balance data, editable at runtime (no redeploy). The rule
    /// engine reads these every time it scores a signature, so DB edits take effect
    /// immediately (ADR 0002 — balance is server-authoritative).
    /// </summary>
    public class DiscoveryTuningSettings
    {
        public int Id { get; set; } // fixed singleton key (1)

        // Significance scoring.
        public int DefaultBehaviorWeight { get; set; }
        public int DefaultFactorWeight { get; set; }
        public int KnowledgeDepthWeight { get; set; }
        public int PersistenceWeight { get; set; }
        public int CombinationSynergy { get; set; }
        public int FireThreshold { get; set; }

        // Power budget curve (continuous in score).
        public int BudgetBase { get; set; }
        // Anti-inflation (ADR 0010): the next discovery in a space costs exponentially more, while
        // the power it buys grows logarithmically.
        public double BudgetGrowth { get; set; }
        public int BudgetMin { get; set; }
        public int BudgetMax { get; set; }

        // Rarity label bands (score thresholds).
        public int UncommonScore { get; set; }
        public int RareScore { get; set; }
        public int EpicScore { get; set; }
        public int LegendaryScore { get; set; }

        // The composition grammar's operators (ADR 0009), scored by prefix — ordered by how deliberate
        // the act is. A composite must be EARNED by composing, never reached by spamming one hand.
        public int FuseWeight { get; set; }
        public int SequenceWeight { get; set; }
        public int ConcurrencyWeight { get; set; }
        public int ChainWeight { get; set; }
    }
}
