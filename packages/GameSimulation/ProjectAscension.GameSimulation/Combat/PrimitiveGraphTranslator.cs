using System.Collections.Generic;
using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Translates a legacy primitive skill into an equivalent effect GRAPH (ADR 0007 Phase 4c-4) —
    /// deterministically, preserving identity (no AI, no re-roll). It's the inverse of the
    /// primitive vocabulary: each primitive maps to the graph node it stands for, under a trigger
    /// chosen by the dominant category (offense/control → OnCast, mobility → OnJumpInAir, defense →
    /// Continuous). So a skill discovered before the graph pipeline still runs entirely on the
    /// graph path — the last graphless case closes, and the primitive runtime fallbacks can retire.
    /// </summary>
    public static class PrimitiveGraphTranslator
    {
        public static EffectNode Translate(Skill skill)
        {
            if (skill == null || skill.Primitives.Count == 0)
                return new Trigger(TriggerKind.Continuous, new Ward(WardEffect.Shield, 0)); // nothing to translate — negligible no-op

            int offense = 0, control = 0, mobility = 0, defense = 0;
            var steps = new List<EffectNode>();
            foreach (var p in skill.Primitives)
            {
                int tier = Tier(p.Magnitude);
                switch (p.Kind)
                {
                    case SkillPrimitiveKind.Projectile: steps.Add(new Emit(EmitDelivery.Projectile, tier)); offense += p.Magnitude; break;
                    case SkillPrimitiveKind.Beam: steps.Add(new Emit(EmitDelivery.Beam, tier)); offense += p.Magnitude; break;
                    case SkillPrimitiveKind.Area: steps.Add(new Emit(EmitDelivery.Burst, tier)); offense += p.Magnitude; break;
                    case SkillPrimitiveKind.DamageOverTime: steps.Add(new Dot(tier, p.Duration)); offense += p.Magnitude; break;
                    case SkillPrimitiveKind.Chain:
                    case SkillPrimitiveKind.Fork:
                    case SkillPrimitiveKind.Pierce: steps.Add(new Effects.Spread(tier)); offense += p.Magnitude; break;
                    case SkillPrimitiveKind.Homing: steps.Add(new Homing(tier)); offense += p.Magnitude; break;
                    case SkillPrimitiveKind.Knockback: steps.Add(new Control(ControlEffect.Knockback, tier)); control += p.Magnitude; break;
                    case SkillPrimitiveKind.Slow: steps.Add(new Control(ControlEffect.Slow, tier)); control += p.Magnitude; break;
                    case SkillPrimitiveKind.Stun: steps.Add(new Control(ControlEffect.Stun, tier)); control += p.Magnitude; break;
                    case SkillPrimitiveKind.Shield: steps.Add(new Ward(WardEffect.Shield, tier)); defense += p.Magnitude; break;
                    case SkillPrimitiveKind.Barrier: steps.Add(new Ward(WardEffect.Barrier, tier)); defense += p.Magnitude; break;
                    case SkillPrimitiveKind.Leech: steps.Add(new Ward(WardEffect.Leech, tier)); defense += p.Magnitude; break;
                    case SkillPrimitiveKind.Dash:
                    case SkillPrimitiveKind.Blink: steps.Add(new Impulse(ImpulseDirection.Up, tier)); mobility += p.Magnitude; break;
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

        // Primitive magnitude (1..5) → graph tier (0..3), the inverse of GraphSkillResolver's tier+1.
        private static int Tier(int magnitude)
        {
            int t = magnitude - 1;
            if (t < 0) t = 0;
            if (t > 3) t = 3;
            return t;
        }
    }
}
