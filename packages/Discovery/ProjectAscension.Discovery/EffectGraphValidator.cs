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
        if (root is not Trigger trigger) return ValidationResult.Fail(CompositionError.InvalidParameter);
        if (!StructureValid(root)) return ValidationResult.Fail(CompositionError.InvalidParameter);
        if (!TiersValid(root)) return ValidationResult.Fail(CompositionError.InvalidMagnitude);
        // Coherence: the trigger's effects must actually DO something at runtime for the
        // manifestation this trigger implies — else the skill validates but is dead (a movement
        // trigger with no impulse, a Continuous with no ward, an OnCast with no real effect). The
        // AI overshoots this too, so reject → it recomposes (the interpreters are the source of
        // truth for what's dead; keep these rules in step with them).
        if (!Coherent(trigger)) return ValidationResult.Fail(CompositionError.InvalidParameter);
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

    // Does the graph produce a real runtime effect for the manifestation its trigger implies?
    // Mirrors the interpreters: MovementCapability grants a jump only from an Impulse under a
    // movement trigger; GraphPassiveResolver reads only Ward under Continuous; GraphSkillResolver
    // needs a damaging/control node to hit. A shape that none of them act on is a dead skill.
    // Does the graph produce a real runtime effect for the manifestation it resolves to? Mirrors
    // the interpreters, so a validated skill is never dead:
    //   - Weapon/Command (offensive/control) → GraphSkillResolver needs a damaging/control node
    //     (Emit/Damage/Dot/Control) or a Ward(Leech) to do anything.
    //   - Passive → GraphPassiveResolver grants reduction/leech from an always-on Ward (not Heal),
    //     OR a movement trigger + an Impulse grants movement (MovementCapability). Otherwise dead.
    // OnHit has no interpreter yet (reserved) — reject until it does.
    private static bool Coherent(Trigger t)
    {
        if (t.Kind == TriggerKind.OnHit) return false;

        var kind = ManifestationFromGraph.Classify(t, magicContext: false) ?? ManifestationKind.Command;
        if (kind == ManifestationKind.Passive)
        {
            bool ward = Has(t.Child, n => n is Ward w && w.Effect != WardEffect.Heal);
            bool moves = t.Kind is TriggerKind.OnJumpInAir or TriggerKind.OnDodge or TriggerKind.OnWallContact
                         && Has(t.Child, n => n is Impulse);
            return ward || moves;
        }
        // Weapon / Command — GraphSkillResolver must produce damage/control (Leech also acts on cast).
        return Has(t.Child, n => n is Emit or Damage or Dot or Control || (n is Ward lw && lw.Effect == WardEffect.Leech));
    }

    private static bool Has(EffectNode node, System.Func<EffectNode, bool> pred)
    {
        if (node is null) return false;
        if (pred(node)) return true;
        return node switch
        {
            Sequence s => s.Steps.Any(n => Has(n, pred)),
            Trigger t => Has(t.Child, pred),
            _ => false,
        };
    }
}
