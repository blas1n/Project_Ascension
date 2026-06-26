namespace ProjectAscension.SkillForge;

/// <summary>
/// What the player actually did: how many times each behavior was performed
/// (server-owned weights turn these into difficulty), and how sustained the pattern
/// was (<see cref="Persistence"/>). The counts are observed facts reported by the
/// client; the significance they carry is decided here, server-side.
/// </summary>
public sealed record BehaviorSignature(IReadOnlyDictionary<string, int> Behaviors, int Persistence);

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
    public static TriggerOutcome Evaluate(BehaviorSignature signature, DiscoveryTuning tuning)
    {
        int frequencyScore = 0;
        int distinct = 0;
        foreach (var (behavior, count) in signature.Behaviors)
        {
            if (count <= 0) continue;
            int weight = tuning.BehaviorWeights.TryGetValue(behavior, out var w) ? w : tuning.DefaultBehaviorWeight;
            frequencyScore += count * weight;
            distinct++;
        }

        int score =
            frequencyScore
            + Math.Max(0, distinct - 1) * tuning.CombinationSynergy
            + signature.Persistence * tuning.PersistenceWeight;

        return new TriggerOutcome(score >= tuning.FireThreshold, RarityFor(score, tuning), score);
    }

    private static Rarity RarityFor(int score, DiscoveryTuning t) =>
        score >= t.LegendaryScore ? Rarity.Legendary
        : score >= t.EpicScore ? Rarity.Epic
        : score >= t.RareScore ? Rarity.Rare
        : score >= t.UncommonScore ? Rarity.Uncommon
        : Rarity.Common;
}
