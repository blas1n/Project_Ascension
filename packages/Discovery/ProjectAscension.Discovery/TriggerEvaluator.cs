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
/// <summary>… and the BAR it had to clear, which rises exponentially with how much the player has
/// already discovered in this space (ADR 0010).</summary>
public sealed record TriggerOutcome(bool Fires, Rarity Rarity, int Score, int Threshold = 0);

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

        // Prior knowledge does NOT add score (ADR 0010). It used to — and that was an inflation vector
        // hiding in plain sight: repeating the SAME play scored higher every time, purely because you
        // had discovered here before, so a player could climb the rarity ladder by doing nothing new.
        // Depth is not a drip of significance.
        //
        // "발견은 다음 발견의 시작" is still honoured — but through LINEAGE (the composer is given the
        // ancestors and evolves them), which enriches what the discovery IS. Not through making it
        // cheaper to get.

        int score =
            behaviorScore
            + contextScore
            + Math.Max(0, distinct - 1) * tuning.CombinationSynergy
            + signature.Persistence * tuning.PersistenceWeight;

        // The LADDER is the progression (see the claim key): a style yields one discovery per rarity
        // rung, and you climb by scoring higher. So the anti-inflation lever is the SPACING of the
        // rungs — they are seeded EXPONENTIALLY (100 / 150 / 225 / 338 / 506, ADR 0010). Repeating an
        // act raises the score roughly linearly, so each further discovery in that style costs
        // exponentially more of it. Grinding exhausts itself; the way up is to compose better (ADR 0009).
        return new TriggerOutcome(score >= tuning.FireThreshold, RarityFor(score, tuning), score, tuning.FireThreshold);
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

    /// <summary>What a given rung demands. The rungs are spaced exponentially (ADR 0010), so each step
    /// up costs far more play than the last.</summary>
    public static int RungScore(Rarity rarity, DiscoveryTuning t) => rarity switch
    {
        Rarity.Legendary => t.LegendaryScore,
        Rarity.Epic => t.EpicScore,
        Rarity.Rare => t.RareScore,
        Rarity.Uncommon => t.UncommonScore,
        _ => t.FireThreshold,
    };

    /// <summary>The rungs of the ladder. Seeded with EXPONENTIAL spacing (ADR 0010), which is what makes
    /// the next discovery in a style cost exponentially more play than the last.</summary>
    private static Rarity RarityFor(int score, DiscoveryTuning t) =>
        score >= t.LegendaryScore ? Rarity.Legendary
        : score >= t.EpicScore ? Rarity.Epic
        : score >= t.RareScore ? Rarity.Rare
        : score >= t.UncommonScore ? Rarity.Uncommon
        : Rarity.Common;
}
