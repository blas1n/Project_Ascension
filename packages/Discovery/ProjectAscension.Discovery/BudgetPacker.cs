namespace ProjectAscension.SkillForge;

/// <summary>
/// Fits an AI-proposed primitive list into a power budget — the rule engine's job,
/// since the numbers (magnitude, range, duration) are deterministic (ADR 0002). The
/// AI chooses the primitives, their order, and intent; this guarantees a balanced,
/// within-budget result without asking the model to do arithmetic it is bad at.
///
/// Two passes preserve variety: first include as many proposed primitives as
/// possible at magnitude 1 / no parameters (breadth), then spend leftover budget
/// raising each primitive's potency and parameters toward what the model asked
/// (depth), round-robin in priority order so it spreads.
/// </summary>
public static class BudgetPacker
{
    public static IReadOnlyList<ComposedPrimitive> Pack(IReadOnlyList<ComposedPrimitive> proposed, PowerBudget budget)
    {
        var kinds = new List<PrimitiveKind>();
        var desiredMag = new List<int>();
        var desiredRange = new List<int>();
        var desiredDuration = new List<int>();
        var mag = new List<int>();
        var range = new List<int>();
        var duration = new List<int>();
        int remaining = budget.Total;

        // Pass 1 — breadth: each known primitive at magnitude 1, no parameters.
        foreach (var p in proposed)
        {
            if (!PrimitiveCatalog.IsKnown(p.Kind)) continue;
            int baseCost = PrimitiveCatalog.BaseCostOf(p.Kind);
            if (baseCost > remaining) continue;

            kinds.Add(p.Kind);
            desiredMag.Add(Math.Clamp(p.Magnitude, 1, CompositionValidator.MaxMagnitude));
            desiredRange.Add(Math.Clamp(p.Range, 0, CompositionValidator.MaxParameterTier));
            desiredDuration.Add(Math.Clamp(p.Duration, 0, CompositionValidator.MaxParameterTier));
            mag.Add(1);
            range.Add(0);
            duration.Add(0);
            remaining -= baseCost;
        }

        // Pass 2 — depth: one affordable upgrade per primitive per round (magnitude,
        // then range, then duration), toward the desired amounts.
        int paramCost = CompositionValidator.ParameterTierCost;
        bool progressed = true;
        while (remaining > 0 && progressed)
        {
            progressed = false;
            for (int i = 0; i < kinds.Count && remaining > 0; i++)
            {
                int baseCost = PrimitiveCatalog.BaseCostOf(kinds[i]);
                if (mag[i] < desiredMag[i] && baseCost <= remaining)
                {
                    mag[i]++;
                    remaining -= baseCost;
                    progressed = true;
                }
                else if (range[i] < desiredRange[i] && paramCost <= remaining)
                {
                    range[i]++;
                    remaining -= paramCost;
                    progressed = true;
                }
                else if (duration[i] < desiredDuration[i] && paramCost <= remaining)
                {
                    duration[i]++;
                    remaining -= paramCost;
                    progressed = true;
                }
            }
        }

        var result = new List<ComposedPrimitive>(kinds.Count);
        for (int i = 0; i < kinds.Count; i++)
            result.Add(new ComposedPrimitive(kinds[i], mag[i], range[i], duration[i]));
        return result;
    }
}
