namespace ProjectAscension.SkillForge;

/// <summary>
/// Fits an AI-proposed primitive list into a power budget — the rule engine's job,
/// since magnitudes are numbers and numbers are deterministic (ADR 0002). The AI
/// chooses the primitives, their order, and intent; this guarantees a balanced,
/// within-budget result without asking the model to do arithmetic it is bad at.
///
/// Two passes preserve the composition's variety: first include as many of the
/// proposed primitives as possible at magnitude 1 (breadth), then spend any
/// leftover budget bumping magnitudes toward what the model asked for (depth), in
/// priority order. This keeps "your own skill" varied instead of collapsing the
/// whole budget into the first primitive.
/// </summary>
public static class BudgetPacker
{
    public static IReadOnlyList<ComposedPrimitive> Pack(IReadOnlyList<ComposedPrimitive> proposed, PowerBudget budget)
    {
        // Pass 1 — breadth: take each known, affordable primitive at magnitude 1,
        // in priority order, while the budget allows.
        var kinds = new List<PrimitiveKind>();
        var desired = new List<int>();
        var magnitude = new List<int>();
        int remaining = budget.Total;

        foreach (var p in proposed)
        {
            if (!PrimitiveCatalog.IsKnown(p.Kind)) continue;
            int baseCost = PrimitiveCatalog.BaseCostOf(p.Kind);
            if (baseCost > remaining) continue;

            kinds.Add(p.Kind);
            desired.Add(Math.Clamp(p.Magnitude, 1, CompositionValidator.MaxMagnitude));
            magnitude.Add(1);
            remaining -= baseCost;
        }

        // Pass 2 — depth: spend leftover budget raising magnitudes toward the
        // desired amounts, round-robin in priority order so it spreads.
        bool progressed = true;
        while (remaining > 0 && progressed)
        {
            progressed = false;
            for (int i = 0; i < kinds.Count && remaining > 0; i++)
            {
                int baseCost = PrimitiveCatalog.BaseCostOf(kinds[i]);
                if (magnitude[i] < desired[i] && baseCost <= remaining)
                {
                    magnitude[i]++;
                    remaining -= baseCost;
                    progressed = true;
                }
            }
        }

        var result = new List<ComposedPrimitive>(kinds.Count);
        for (int i = 0; i < kinds.Count; i++)
            result.Add(new ComposedPrimitive(kinds[i], magnitude[i]));
        return result;
    }
}
