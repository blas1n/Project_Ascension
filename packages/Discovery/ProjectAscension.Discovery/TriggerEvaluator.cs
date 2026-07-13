namespace ProjectAscension.SkillForge;

/// <summary>
/// What the player actually did and where: how many times each behavior was
/// performed (<see cref="Behaviors"/>), the surrounding context factors —
/// environment / equipment / knowledge (<see cref="Factors"/>), how much relevant
/// prior knowledge the player already owns here (<see cref="KnowledgeDepth"/>), and
/// how sustained the pattern was (<see cref="Persistence"/>). The counts and factors
/// are observed facts reported by the client; the significance they carry is decided
/// here, server-side.
/// </summary>
public sealed record BehaviorSignature(
    IReadOnlyDictionary<string, int> Behaviors,
    IReadOnlyList<string> Factors,
    int KnowledgeDepth,
    int Persistence);

/// <summary>Whether a behavior signature fires a discovery, the derived rarity, and
/// the raw significance score (for observability/tuning).</summary>
public sealed record TriggerOutcome(bool Fires, Rarity Rarity, int Score);

/// <summary>
/// The deterministic discovery trigger (ADR 0002 core 4) — a significance-scoring
/// FUNCTION over the actual behavior combination, not a closed catalog. Each
/// behavior contributes count × its weight, distinct behaviors add a combination
/// synergy, and sustained play adds persistence. Once the score crosses the
/// threshold a discovery fires, with rarity (and, via <see cref="BudgetRules"/>,
/// power) derived from the score. All weights/thresholds come from
/// <see cref="DiscoveryTuning"/>, so balance is data-driven, not hard-coded.
///
/// Honest boundary: the axes and weights are authored, so discoveries are open only
/// within the dimensions the engine measures — but which behaviors are combined, and
/// how much, drives the score continuously, so novel combinations fire and score
/// differently without being enumerated.
/// </summary>
public static class TriggerEvaluator
{
    /// <summary>Composite behaviours (ADR 0009) — the grammar's four operators. Scored by PREFIX, so a
    /// new weapon, a new act, or a combination nobody thought of opens up without seeding a single row.
    /// Mirrors GameSimulation's CompositionDeriver.</summary>
    public const string FusePrefix = "Fuse:";    // almost the same instant — the tightest mastery
    public const string SeqPrefix = "Seq:";      // one act flowing into the next
    public const string WhilePrefix = "While:";  // done while some quality held
    public const string ChainPrefix = "Chain:";  // done again and again

    public static TriggerOutcome Evaluate(BehaviorSignature signature, DiscoveryTuning tuning)
    {
        int behaviorScore = 0;
        int distinct = 0;
        foreach (var (behavior, count) in signature.Behaviors)
        {
            if (count <= 0) continue;
            // A composite is scored as a composite (ADR 0009). Tightness is the signal: fusing two acts
            // in an instant is a harder, more deliberate thing than stringing them together, and is
            // worth more than either — and far more than doing one thing many times.
            int weight = Composite(behavior, tuning) ?? (
                tuning.BehaviorWeights.TryGetValue(behavior, out var w) ? w : tuning.DefaultBehaviorWeight);
            behaviorScore += count * weight;
            distinct++;
        }

        // Context factors (environment / equipment / knowledge) add their own
        // significance, and combining them with the behavior counts toward synergy —
        // the 5-factor interaction that makes the same behavior discover differently
        // by where and how it happens (discovery.md).
        int contextScore = 0;
        foreach (var factor in signature.Factors)
        {
            int weight = tuning.FactorWeights.TryGetValue(factor, out var w) ? w : tuning.DefaultFactorWeight;
            if (weight == 0) continue;
            contextScore += weight;
            distinct++;
        }

        // Prior knowledge the player owns in this space deepens a discovery — mastered
        // discoveries are the material for the next one (discovery.md 발견 그래프:
        // "발견은 다음 발견의 시작"). Depth adds significance and counts as an element.
        int depthScore = signature.KnowledgeDepth * tuning.KnowledgeDepthWeight;
        if (signature.KnowledgeDepth > 0) distinct++;

        int score =
            behaviorScore
            + contextScore
            + depthScore
            + Math.Max(0, distinct - 1) * tuning.CombinationSynergy
            + signature.Persistence * tuning.PersistenceWeight;

        return new TriggerOutcome(score >= tuning.FireThreshold, RarityFor(score, tuning), score);
    }

    /// <summary>The weight of a composite behaviour, or null if it is a plain count.</summary>
    private static int? Composite(string behavior, DiscoveryTuning t)
    {
        if (behavior.StartsWith(FusePrefix, StringComparison.Ordinal)) return t.FuseWeight;
        if (behavior.StartsWith(SeqPrefix, StringComparison.Ordinal)) return t.SequenceWeight;
        if (behavior.StartsWith(WhilePrefix, StringComparison.Ordinal)) return t.ConcurrencyWeight;
        if (behavior.StartsWith(ChainPrefix, StringComparison.Ordinal)) return t.ChainWeight;
        return null;
    }

    private static Rarity RarityFor(int score, DiscoveryTuning t) =>
        score >= t.LegendaryScore ? Rarity.Legendary
        : score >= t.EpicScore ? Rarity.Epic
        : score >= t.RareScore ? Rarity.Rare
        : score >= t.UncommonScore ? Rarity.Uncommon
        : Rarity.Common;
}
