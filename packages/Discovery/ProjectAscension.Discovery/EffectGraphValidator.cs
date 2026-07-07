using System.Linq;

namespace ProjectAscension.SkillForge;

/// <summary>
/// Validates a composed effect graph (ADR 0007): a <see cref="Trigger"/> at the root, no nested
/// triggers or empty sequences, tiers in range, bounded node count, and within the power budget.
/// Same contract as <see cref="CompositionValidator"/> — invalid output makes the discovery
/// defer, with no deterministic fallback (ADR 0002).
/// </summary>
public static class EffectGraphValidator
{
    public static ValidationResult Validate(EffectNode root, PowerBudget budget)
    {
        if (root is null) return ValidationResult.Fail(CompositionError.EmptyComposition);
        // A skill fires on a trigger; the root must be one.
        if (root is not Trigger) return ValidationResult.Fail(CompositionError.InvalidParameter);
        if (!StructureValid(root)) return ValidationResult.Fail(CompositionError.InvalidParameter);
        if (!TiersValid(root)) return ValidationResult.Fail(CompositionError.InvalidMagnitude);
        if (EffectGraph.NodeCount(root) > EffectGraph.MaxNodes) return ValidationResult.Fail(CompositionError.OverBudget);

        int cost = EffectGraph.Cost(root);
        return cost > budget.Total
            ? ValidationResult.Fail(CompositionError.OverBudget, cost)
            : ValidationResult.Ok(cost);
    }

    // A trigger's child must exist and not be another trigger; a sequence must be non-empty and
    // hold only (non-trigger) effect nodes.
    private static bool StructureValid(EffectNode node) => node switch
    {
        Trigger t => t.Child is not null && t.Child is not Trigger && StructureValid(t.Child),
        Sequence s => s.Steps is { Count: > 0 } && s.Steps.All(n => n is not null && n is not Trigger && StructureValid(n)),
        null => false,
        _ => true, // leaf effect
    };

    private static bool TiersValid(EffectNode node) => node switch
    {
        Trigger t => TiersValid(t.Child),
        Sequence s => s.Steps.All(TiersValid),
        Emit e => InRange(e.Tier),
        Impulse i => InRange(i.Tier),
        Damage d => InRange(d.Tier),
        Control c => InRange(c.Tier),
        Ward w => InRange(w.Tier),
        Dot dot => InRange(dot.Tier) && dot.Duration >= 0,
        Spread sp => InRange(sp.Tier),
        Homing h => InRange(h.Tier),
        _ => true,
    };

    private static bool InRange(int tier) => tier >= 0 && tier <= EffectGraph.MaxTier;
}
