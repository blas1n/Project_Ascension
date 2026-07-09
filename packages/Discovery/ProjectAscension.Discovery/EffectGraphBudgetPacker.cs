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

        int guard = 64; // bounded: tiers are 0..MaxTier over <=8 nodes
        while (Cost(steps) > budget.Total && guard-- > 0)
        {
            int idx = IndexOfCostliest(steps);
            if (idx < 0) break;

            var lower = LowerTier(steps[idx]);
            if (lower is not null)
                steps[idx] = lower;
            else if (steps.Count > 1)
                steps.RemoveAt(idx); // already tier 0 — drop it, but never empty the effect
            else
                break; // one tier-0 node left; can't shrink further
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
    private static EffectNode? LowerTier(EffectNode node) => node switch
    {
        Emit e when e.Tier > 0 => new Emit(e.Delivery, e.Tier - 1),
        Impulse i when i.Tier > 0 => new Impulse(i.Direction, i.Tier - 1),
        Damage d when d.Tier > 0 => new Damage(d.Tier - 1),
        Control c when c.Tier > 0 => new Control(c.Effect, c.Tier - 1),
        Ward w when w.Tier > 0 => new Ward(w.Effect, w.Tier - 1),
        Dot dot when dot.Tier > 0 => new Dot(dot.Tier - 1, dot.Duration),
        Spread sp when sp.Tier > 0 => new Spread(sp.Tier - 1),
        Homing h when h.Tier > 0 => new Homing(h.Tier - 1),
        _ => null,
    };
}
