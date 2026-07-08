using System.Collections.Generic;

namespace ProjectAscension.SkillForge;

/// <summary>
/// Server-side deterministic translation of a legacy primitive composition into an equivalent
/// effect GRAPH (ADR 0007 Phase 4c-4 backfill) — the mirror of the client's PrimitiveGraphTranslator.
/// Used to backfill a graph onto discoveries composed before the graph pipeline, preserving their
/// identity (no AI, no re-roll), so `PrimitivesJson` and the graphless runtime fallbacks can retire.
/// </summary>
public static class PrimitiveGraphTranslator
{
    public static EffectNode Translate(IReadOnlyList<ComposedPrimitive> primitives)
    {
        if (primitives is null || primitives.Count == 0)
            return new Trigger(TriggerKind.Continuous, new Ward(WardEffect.Shield, 0));

        int offense = 0, control = 0, mobility = 0, defense = 0;
        var steps = new List<EffectNode>();
        foreach (var p in primitives)
        {
            int tier = Tier(p.Magnitude);
            switch (p.Kind)
            {
                case PrimitiveKind.Projectile: steps.Add(new Emit(EmitDelivery.Projectile, tier)); offense += p.Magnitude; break;
                case PrimitiveKind.Beam: steps.Add(new Emit(EmitDelivery.Beam, tier)); offense += p.Magnitude; break;
                case PrimitiveKind.Area: steps.Add(new Emit(EmitDelivery.Burst, tier)); offense += p.Magnitude; break;
                case PrimitiveKind.DamageOverTime: steps.Add(new Dot(tier, p.Duration)); offense += p.Magnitude; break;
                case PrimitiveKind.Chain:
                case PrimitiveKind.Fork:
                case PrimitiveKind.Pierce: steps.Add(new Spread(tier)); offense += p.Magnitude; break;
                case PrimitiveKind.Homing: steps.Add(new Homing(tier)); offense += p.Magnitude; break;
                case PrimitiveKind.Knockback: steps.Add(new Control(ControlEffect.Knockback, tier)); control += p.Magnitude; break;
                case PrimitiveKind.Slow: steps.Add(new Control(ControlEffect.Slow, tier)); control += p.Magnitude; break;
                case PrimitiveKind.Stun: steps.Add(new Control(ControlEffect.Stun, tier)); control += p.Magnitude; break;
                case PrimitiveKind.Shield: steps.Add(new Ward(WardEffect.Shield, tier)); defense += p.Magnitude; break;
                case PrimitiveKind.Barrier: steps.Add(new Ward(WardEffect.Barrier, tier)); defense += p.Magnitude; break;
                case PrimitiveKind.Leech: steps.Add(new Ward(WardEffect.Leech, tier)); defense += p.Magnitude; break;
                case PrimitiveKind.Dash:
                case PrimitiveKind.Blink: steps.Add(new Impulse(ImpulseDirection.Up, tier)); mobility += p.Magnitude; break;
            }
        }

        if (steps.Count == 0) return new Trigger(TriggerKind.Continuous, new Ward(WardEffect.Shield, 0));

        // Same taxonomy as manifestation: the dominant category picks the trigger.
        TriggerKind trigger =
            mobility >= offense && mobility >= control && mobility >= defense ? TriggerKind.OnJumpInAir :
            defense > offense && defense > control ? TriggerKind.Continuous :
            TriggerKind.OnCast;

        EffectNode effect = steps.Count == 1 ? steps[0] : new Sequence(steps);
        return new Trigger(trigger, effect);
    }

    // Primitive magnitude (1..5) → graph tier (0..3).
    private static int Tier(int magnitude)
    {
        int t = magnitude - 1;
        if (t < 0) t = 0;
        if (t > EffectGraph.MaxTier) t = EffectGraph.MaxTier;
        return t;
    }
}
