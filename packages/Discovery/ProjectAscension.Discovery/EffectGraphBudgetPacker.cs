using System.Collections.Generic;
using System.Linq;

namespace ProjectAscension.SkillForge;

/// <summary>
/// Fits an AI-composed effect graph into its power budget (ADR 0002 — the AI proposes STRUCTURE,
/// the engine owns the NUMBERS). Models overshoot tight budgets: they compose the right shape but
/// with tiers that cost too much, so validation rejects every attempt and the discovery defers
/// forever. Rather than trust the model to hit an exact number, the engine clamps it down
/// deterministically: repeatedly lower the most expensive effect's tier, and only drop an effect
/// when it is already at tier 0 and the graph still doesn't fit. Structure is preserved as far as
/// possible; the discovery always composes.
/// </summary>
public static class EffectGraphBudgetPacker
{
    public static EffectNode Pack(EffectNode root, PowerBudget budget)
    {
        if (root is not Trigger trigger) return root; // not our shape — the validator will reject it

        var steps = trigger.Child is Sequence seq ? seq.Steps.ToList() : new List<EffectNode> { trigger.Child };

        // An over-budget skill loses an EFFECT, never a number (ADR 0010). The old packer shaved tiers
        // first, which meant a skill you couldn't afford simply hit softer — magnitude was the currency.
        // But the numbers were never supposed to be for sale: what a modest discovery gives up is what it
        // can DO, not how hard it hits. So drop the most expensive effect and keep the rest whole.
        int guard = EffectGraph.MaxNodes + 1;
        while (Cost(steps) > budget.Total && steps.Count > 1 && guard-- > 0)
        {
            int idx = IndexOfCostliest(steps);
            if (idx < 0) break;
            steps.RemoveAt(idx);
        }

        EffectNode child = steps.Count == 1 ? steps[0] : new Sequence(steps);
        return new Trigger(trigger.Kind, child);
    }

    private static int Cost(IReadOnlyList<EffectNode> steps)
    {
        int total = 0;
        foreach (var n in steps) total += EffectGraph.Cost(n);
        return total;
    }

    private static int IndexOfCostliest(IReadOnlyList<EffectNode> steps)
    {
        int best = -1, bestCost = -1;
        for (int i = 0; i < steps.Count; i++)
        {
            int c = EffectGraph.Cost(steps[i]);
            if (c > bestCost) { bestCost = c; best = i; }
        }
        return best;
    }

    // A copy of the node one tier lower, or null when it is already at tier 0 (structural nodes
    // like a nested Sequence aren't lowered — they shouldn't appear as steps).
}
