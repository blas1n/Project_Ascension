namespace ProjectAscension.SkillForge;

/// <summary>How significant a discovery is — a label derived from its significance
/// score, also usable to request a representative budget on the manual trigger
/// path.</summary>
public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

/// <summary>
/// Derives a discovery's power budget deterministically — the rule engine owns this
/// number, never the client or the AI (ADR 0002; CLAUDE.md AI rules). The budget
/// scales continuously with the significance score, so a stronger behavior pattern
/// yields a richer skill (not just at rarity-tier boundaries). All coefficients come
/// from <see cref="DiscoveryTuning"/>, so balance is tunable at runtime.
/// </summary>
public static class BudgetRules
{
    /// <summary>
    /// Budget from a significance score — LOGARITHMIC, deliberately (ADR 0010). A stronger pattern
    /// still yields a richer skill, but a 60x score buys roughly a 1.6x budget.
    ///
    /// This is the other half of the anti-inflation pair: the COST of the next discovery in a space
    /// grows exponentially, while the POWER it buys grows logarithmically. Getting stronger is never
    /// forbidden — it is only made steadily, and then punishingly, more expensive. A player who only
    /// repeats themselves does not hit a wall; they hit a curve, and feel it long before it stops them.
    /// </summary>
    public static PowerBudget FromScore(int score, DiscoveryTuning tuning)
    {
        double magnitude = Math.Log2(1 + Math.Max(0, score));
        int budget = tuning.BudgetBase + (int)Math.Round(tuning.BudgetGrowth * magnitude);
        return new PowerBudget(Math.Clamp(budget, tuning.BudgetMin, tuning.BudgetMax));
    }

    /// <summary>Budget for a manually-specified rarity (the non-scored trigger path):
    /// maps the rarity to a representative score, then uses the same curve.</summary>
    public static PowerBudget FromRarity(Rarity rarity, DiscoveryTuning tuning)
        => FromScore(RepresentativeScore(rarity, tuning), tuning);

    private static int RepresentativeScore(Rarity rarity, DiscoveryTuning t) => rarity switch
    {
        Rarity.Legendary => t.LegendaryScore,
        Rarity.Epic => t.EpicScore,
        Rarity.Rare => t.RareScore,
        Rarity.Uncommon => t.UncommonScore,
        _ => t.FireThreshold,
    };
}
