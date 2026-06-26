namespace ProjectAscension.SkillForge;

/// <summary>How significant a discovery is — the rule engine's classification that
/// drives its power budget. (For now supplied at trigger time; later derived from
/// behavior rarity/difficulty, ADR 0002 core 4.)</summary>
public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

/// <summary>
/// Derives a discovery's power budget deterministically from its rarity — the rule
/// engine owns this number, never the client or the AI (ADR 0002; CLAUDE.md AI
/// rules). Centralizing it here keeps balance server-authoritative and tunable.
/// </summary>
public static class BudgetRules
{
    public static PowerBudget Derive(Rarity rarity) => new(rarity switch
    {
        Rarity.Common => 20,
        Rarity.Uncommon => 26,
        Rarity.Rare => 32,
        Rarity.Epic => 40,
        Rarity.Legendary => 50,
        _ => 20,
    });
}
