namespace ProjectAscension.SkillForge;

/// <summary>
/// The tunable knobs the discovery rule engine scores against — behavior weights,
/// scoring coefficients, the power-budget curve, and rarity bands. SkillForge stays
/// pure: the host loads these (from the DB, at runtime) and passes them in, so
/// balance is data-driven and server-authoritative (ADR 0002), never hard-coded.
/// </summary>
public sealed record DiscoveryTuning(
    IReadOnlyDictionary<string, int> BehaviorWeights,
    int DefaultBehaviorWeight,
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
    int LegendaryScore)
{
    /// <summary>A baseline used by tests and as a safe fallback when the DB has no
    /// tuning row yet. Mirrors the seeded defaults.</summary>
    public static DiscoveryTuning Default { get; } = new(
        new Dictionary<string, int>
        {
            ["Jump"] = 1,
            ["Dodge"] = 1,
            ["MeleeAttack"] = 2,
            ["RangedAttack"] = 2,
            ["ChargeAttack"] = 3,
            ["DodgeAttack"] = 3,
        },
        DefaultBehaviorWeight: 1,
        PersistenceWeight: 5,
        CombinationSynergy: 15,
        FireThreshold: 100,
        BudgetBase: 8,
        BudgetPerScore: 0.18,
        BudgetMin: 16,
        BudgetMax: 64,
        UncommonScore: 120,
        RareScore: 150,
        EpicScore: 200,
        LegendaryScore: 250);
}
