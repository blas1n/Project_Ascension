using System;
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Resolves a discovered skill's EFFECT GRAPH (ADR 0007 Phase 4b) into the same deterministic
    /// <see cref="SkillResolution"/> that <see cref="SkillResolver"/> produces from flat primitives
    /// — so an AI-composed graph drives real combat with no loss of variety. Same accumulators,
    /// same <see cref="CombatTuning"/> weights (DB-driven), same per-target math; only the input
    /// encoding differs (tier → magnitude via <c>tier + 1</c>, mirroring the graph's cost model).
    ///
    /// The graph separates concerns the primitives conflated: <see cref="Emit"/> sets the delivery
    /// shape + base damage, <see cref="Spread"/> the extra targets, <see cref="Dot"/> the burn,
    /// <see cref="Homing"/> is a targeting aid (no numbers). Only the offensive/utility effects
    /// under the trigger contribute; movement triggers/impulses are handled elsewhere
    /// (MovementCapability).
    /// </summary>
    public static class GraphSkillResolver
    {
        public static SkillResolution Resolve(EffectNode graph, int availableTargets, CombatTuning tuning = null)
        {
            if (graph is not Trigger trigger || availableTargets <= 0) return SkillResolution.Empty;
            var t = tuning ?? CombatTuning.Default;

            float single = 0f, area = 0f, dotPerTick = 0f;
            int dotDuration = 0, spread = 0, leech = 0, shield = 0, dash = 0;
            var control = ControlKind.None;
            int controlMagnitude = 0;
            // A burst/nova Emit opens the AREA channel; subsequent Damage lands there too.
            bool areaMode = HasAreaEmit(trigger.Child);

            void Accumulate(EffectNode node)
            {
                switch (node)
                {
                    case Sequence s:
                        foreach (var step in s.Steps) Accumulate(step);
                        break;
                    case Emit e:
                        {
                            int mag = e.Tier + 1;
                            switch (e.Delivery)
                            {
                                case EmitDelivery.Projectile: single += mag * t.ProjectileDamage; break;
                                case EmitDelivery.Beam: single += mag * t.BeamDamage; break;
                                case EmitDelivery.Burst:
                                case EmitDelivery.Nova: area += mag * t.AreaDamage; break;
                            }
                            break;
                        }
                    case Damage d:
                        {
                            int mag = d.Tier + 1;
                            if (areaMode) area += mag * t.AreaDamage;
                            else single += mag * t.ProjectileDamage;
                            break;
                        }
                    case Dot dot:
                        dotPerTick += (dot.Tier + 1) * t.DotDamagePerTick;
                        dotDuration = Math.Max(dotDuration, dot.Duration);
                        break;
                    case Effects.Spread sp:
                        spread += sp.Tier + 1;
                        break;
                    case Control c:
                        Promote(ref control, ref controlMagnitude, c.Effect switch
                        {
                            ControlEffect.Knockback => ControlKind.Knockback,
                            ControlEffect.Slow => ControlKind.Slow,
                            _ => ControlKind.Stun,
                        }, c.Tier + 1);
                        break;
                    case Ward w:
                        int wm = w.Tier + 1;
                        if (w.Effect == WardEffect.Leech) leech += wm;
                        else shield += wm; // Shield / Barrier / Heal → protective/sustain value
                        break;
                    case Impulse imp:
                        dash += imp.Tier + 1; // a dash/blink woven into a cast (e.g. a lunge attack)
                        break;
                        // Homing: targeting aid only, no numbers.
                }
            }

            Accumulate(trigger.Child);

            bool hasArea = area > 0f;
            int dotTicks = (dotPerTick > 0f) ? t.BaseDotTicks + dotDuration : 0;
            float controlDuration = controlMagnitude * t.ControlDurationPerMagnitude;
            float controlStrength = control switch
            {
                ControlKind.Slow => controlMagnitude * t.SlowPerMagnitude,
                ControlKind.Knockback => controlMagnitude * t.KnockbackPerMagnitude,
                _ => 0f,
            };
            int hitCount = hasArea ? availableTargets : Math.Min(availableTargets, 1 + spread);

            var hits = new List<TargetEffect>(hitCount);
            float directTotal = 0f;
            for (int i = 0; i < hitCount; i++)
            {
                float dmg = 0f;
                if (i == 0) dmg += single;                        // primary takes the focused hit
                else if (i <= spread) dmg += single * t.SpreadFalloff; // chained/pierced
                if (hasArea) dmg += area;

                if (dmg <= 0f && dotPerTick <= 0f && control == ControlKind.None) continue;

                directTotal += dmg;
                hits.Add(new TargetEffect(i, dmg, dotPerTick, dotTicks, control, controlDuration, controlStrength));
            }

            float selfHeal = directTotal * (leech * t.LeechFractionPerMagnitude);
            return new SkillResolution(hits, selfHeal, shield * t.ShieldPerMagnitude, dash * t.DashPerMagnitude);
        }

        private static bool HasAreaEmit(EffectNode node)
        {
            switch (node)
            {
                case Emit e: return e.Delivery == EmitDelivery.Burst || e.Delivery == EmitDelivery.Nova;
                case Sequence s:
                    foreach (var step in s.Steps)
                        if (HasAreaEmit(step)) return true;
                    return false;
                default: return false;
            }
        }

        // The strongest control wins (Stun > Slow > Knockback); keep its magnitude for duration.
        private static void Promote(ref ControlKind control, ref int magnitude, ControlKind kind, int mag)
        {
            if ((int)kind > (int)control) { control = kind; magnitude = mag; }
        }
    }
}
