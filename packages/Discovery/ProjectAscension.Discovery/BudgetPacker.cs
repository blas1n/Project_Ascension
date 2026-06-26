namespace ProjectAscension.SkillForge;

/// <summary>
/// Fits an AI-proposed primitive list into a power budget — the rule engine's job,
/// since magnitudes are numbers and numbers are deterministic (ADR 0002). Walks the
/// proposal in priority order, clamping each magnitude down so the running total
/// never exceeds the budget and skipping primitives that don't fit even at
/// magnitude 1. The AI chooses the primitives, their order, and intent; this
/// guarantees a balanced, within-budget result so composition reliably succeeds
/// without asking the model to do arithmetic it is bad at.
/// </summary>
public static class BudgetPacker
{
    public static IReadOnlyList<ComposedPrimitive> Pack(IReadOnlyList<ComposedPrimitive> proposed, PowerBudget budget)
    {
        var result = new List<ComposedPrimitive>();
        int remaining = budget.Total;

        foreach (var p in proposed)
        {
            if (!PrimitiveCatalog.IsKnown(p.Kind)) continue;

            int baseCost = PrimitiveCatalog.BaseCostOf(p.Kind);
            if (baseCost > remaining) continue; // can't afford even magnitude 1

            int desired = p.Magnitude < 1 ? 1 : p.Magnitude;
            int magnitude = Math.Min(Math.Min(desired, CompositionValidator.MaxMagnitude), remaining / baseCost);

            result.Add(new ComposedPrimitive(p.Kind, magnitude));
            remaining -= baseCost * magnitude;
        }

        return result;
    }
}
