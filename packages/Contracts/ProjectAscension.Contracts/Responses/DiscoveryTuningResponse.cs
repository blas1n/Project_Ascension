#nullable enable
using System.Collections.Generic;

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>The current discovery balance values (read-only view), for inspection
    /// and verifying runtime edits.</summary>
    public record DiscoveryTuningResponse(
        IReadOnlyDictionary<string, int> BehaviorWeights,
        IReadOnlyDictionary<string, int> FactorWeights,
        int DefaultBehaviorWeight,
        int DefaultFactorWeight,
        int KnowledgeDepthWeight,
        int PersistenceWeight,
        int CombinationSynergy,
        int FireThreshold,
        int BudgetBase,
        double BudgetPerScore,
        int BudgetMin,
        int BudgetMax,
        int UncommonScore,
        int RareScore,
        int EpicScore,
        int LegendaryScore);
}
