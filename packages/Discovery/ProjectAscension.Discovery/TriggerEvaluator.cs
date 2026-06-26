namespace ProjectAscension.SkillForge;

/// <summary>
/// The accumulated signals for a behavior pattern — the axes the rule engine scores
/// (discovery.md 행동 기반 발견): how often (<see cref="Frequency"/>), how sustained
/// (<see cref="Persistence"/>), how hard (<see cref="Difficulty"/>), and how many
/// distinct behaviors are combined (<see cref="Combination"/>, 1 = a single behavior).
/// </summary>
public sealed record BehaviorSignature(int Frequency, int Persistence, int Difficulty, int Combination);

/// <summary>Whether a behavior signature fires a discovery, the derived rarity, and
/// the raw significance score (for observability/tuning).</summary>
public sealed record TriggerOutcome(bool Fires, Rarity Rarity, int Score);

/// <summary>
/// The deterministic discovery trigger (ADR 0002 core 4) — a significance-scoring
/// FUNCTION, not a closed catalog. It scores a behavior signature; once the score
/// crosses <see cref="FireThreshold"/> a discovery fires, with rarity derived from
/// the score. Same signature → same outcome (server-authoritative, reproducible).
///
/// Honest boundary: the axes, weights, and thresholds are authored, so discoveries
/// are open only within the dimensions the engine measures — but the fire/rarity
/// decision is continuous over the signature space, so genuinely novel behavior
/// combinations fire without being enumerated in a table.
/// </summary>
public static class TriggerEvaluator
{
    public const int FireThreshold = 100;

    public static TriggerOutcome Evaluate(BehaviorSignature signature)
    {
        // Frequency accrues slowly; rarity comes mostly from difficulty, sustained
        // persistence, and combining multiple behaviors.
        int score =
            signature.Frequency
            + signature.Persistence * 5
            + signature.Difficulty * 10
            + Math.Max(0, signature.Combination - 1) * 15;

        return new TriggerOutcome(score >= FireThreshold, RarityFor(score), score);
    }

    private static Rarity RarityFor(int score) => score switch
    {
        >= 250 => Rarity.Legendary,
        >= 200 => Rarity.Epic,
        >= 150 => Rarity.Rare,
        >= 120 => Rarity.Uncommon,
        _ => Rarity.Common,
    };
}
