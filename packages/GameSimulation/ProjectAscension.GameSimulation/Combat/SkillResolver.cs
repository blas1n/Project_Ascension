using System;
using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Turns a discovered <see cref="Skill"/> into deterministic combat effects — the
    /// layer that makes an AI-composed skill actually do something. Each effect
    /// primitive maps to a combat behavior (damage, area, chain, damage-over-time,
    /// leech, shield, control, mobility); numbers are derived from magnitude/range/
    /// duration with fixed weights so the result is reproducible and balance-tunable.
    ///
    /// Spatial hit detection stays in Unity: the caller passes how many targets are in
    /// range (<c>availableTargets</c>, index 0 = the primary) and applies the returned
    /// per-target damage via <see cref="CombatResolver"/>.
    /// </summary>
    public static class SkillResolver
    {
        // Per-magnitude weights come from CombatTuning (DB-driven); Default mirrors the
        // seeded values, so existing callers/tests keep the same numbers.
        public static SkillResolution Resolve(Skill skill, int availableTargets, CombatTuning tuning = null)
        {
            if (skill.Primitives.Count == 0 || availableTargets <= 0) return SkillResolution.Empty;
            var t = tuning ?? CombatTuning.Default;

            float single = 0f;     // focused single-target damage (projectile/beam)
            float area = 0f;       // damage to every target in range
            float dotPerTick = 0f;
            int dotDuration = 0;
            int spread = 0;        // how many extra targets the focused damage reaches
            int leech = 0, shield = 0, dash = 0;
            var control = ControlKind.None;
            int controlMagnitude = 0;

            foreach (var p in skill.Primitives)
            {
                switch (p.Kind)
                {
                    case SkillPrimitiveKind.Projectile: single += p.Magnitude * t.ProjectileDamage; spread += p.Range; break;
                    case SkillPrimitiveKind.Beam: single += p.Magnitude * t.BeamDamage; spread += p.Range; break;
                    case SkillPrimitiveKind.Area: area += p.Magnitude * t.AreaDamage; break;
                    case SkillPrimitiveKind.DamageOverTime:
                        dotPerTick += p.Magnitude * t.DotDamagePerTick;
                        dotDuration = Math.Max(dotDuration, p.Duration);
                        break;
                    case SkillPrimitiveKind.Chain:
                    case SkillPrimitiveKind.Fork:
                    case SkillPrimitiveKind.Pierce: spread += p.Magnitude + p.Range; break;
                    case SkillPrimitiveKind.Knockback: Promote(ref control, ref controlMagnitude, ControlKind.Knockback, p.Magnitude); break;
                    case SkillPrimitiveKind.Slow: Promote(ref control, ref controlMagnitude, ControlKind.Slow, p.Magnitude); break;
                    case SkillPrimitiveKind.Stun: Promote(ref control, ref controlMagnitude, ControlKind.Stun, p.Magnitude); break;
                    case SkillPrimitiveKind.Shield:
                    case SkillPrimitiveKind.Barrier: shield += p.Magnitude; break;
                    case SkillPrimitiveKind.Dash:
                    case SkillPrimitiveKind.Blink: dash += p.Magnitude; break;
                    case SkillPrimitiveKind.Leech: leech += p.Magnitude; break;
                    case SkillPrimitiveKind.Homing: break; // targeting aid; no direct numbers
                }
            }

            bool hasArea = area > 0f;
            int dotTicks = (dotPerTick > 0f) ? t.BaseDotTicks + dotDuration : 0;
            float controlDuration = controlMagnitude * t.ControlDurationPerMagnitude;
            int hitCount = hasArea ? availableTargets : Math.Min(availableTargets, 1 + spread);

            var hits = new List<TargetEffect>(hitCount);
            float directTotal = 0f;
            for (int i = 0; i < hitCount; i++)
            {
                float dmg = 0f;
                if (i == 0) dmg += single;                       // primary takes the focused hit
                else if (i <= spread) dmg += single * t.SpreadFalloff; // chained/pierced
                if (hasArea) dmg += area;

                if (dmg <= 0f && dotPerTick <= 0f && control == ControlKind.None) continue;

                directTotal += dmg;
                hits.Add(new TargetEffect(i, dmg, dotPerTick, dotTicks, control, controlDuration));
            }

            float selfHeal = directTotal * (leech * t.LeechFractionPerMagnitude);
            return new SkillResolution(hits, selfHeal, shield * t.ShieldPerMagnitude, dash * t.DashPerMagnitude);
        }

        // The strongest control wins (Stun > Slow > Knockback); keep its magnitude for duration.
        private static void Promote(ref ControlKind control, ref int magnitude, ControlKind kind, int mag)
        {
            if ((int)kind > (int)control) { control = kind; magnitude = mag; }
        }
    }
}
