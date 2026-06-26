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
        // Damage weights (per magnitude) — combat balance constants.
        private const float ProjectileDamage = 10f;
        private const float BeamDamage = 9f;
        private const float AreaDamage = 8f;
        private const float DotDamagePerTick = 3f;
        private const float SpreadFalloff = 0.6f;   // chained/pierced targets take less
        private const int BaseDotTicks = 2;          // + duration tier
        private const float ShieldPerMagnitude = 12f;
        private const float DashPerMagnitude = 2f;
        private const float LeechFractionPerMagnitude = 0.15f;

        public static SkillResolution Resolve(Skill skill, int availableTargets)
        {
            if (skill.Primitives.Count == 0 || availableTargets <= 0) return SkillResolution.Empty;

            float single = 0f;     // focused single-target damage (projectile/beam)
            float area = 0f;       // damage to every target in range
            float dotPerTick = 0f;
            int dotDuration = 0;
            int spread = 0;        // how many extra targets the focused damage reaches
            int leech = 0, shield = 0, dash = 0;
            var control = ControlKind.None;

            foreach (var p in skill.Primitives)
            {
                switch (p.Kind)
                {
                    case SkillPrimitiveKind.Projectile: single += p.Magnitude * ProjectileDamage; spread += p.Range; break;
                    case SkillPrimitiveKind.Beam: single += p.Magnitude * BeamDamage; spread += p.Range; break;
                    case SkillPrimitiveKind.Area: area += p.Magnitude * AreaDamage; break;
                    case SkillPrimitiveKind.DamageOverTime:
                        dotPerTick += p.Magnitude * DotDamagePerTick;
                        dotDuration = Math.Max(dotDuration, p.Duration);
                        break;
                    case SkillPrimitiveKind.Chain:
                    case SkillPrimitiveKind.Fork:
                    case SkillPrimitiveKind.Pierce: spread += p.Magnitude + p.Range; break;
                    case SkillPrimitiveKind.Knockback: control = Max(control, ControlKind.Knockback); break;
                    case SkillPrimitiveKind.Slow: control = Max(control, ControlKind.Slow); break;
                    case SkillPrimitiveKind.Stun: control = Max(control, ControlKind.Stun); break;
                    case SkillPrimitiveKind.Shield:
                    case SkillPrimitiveKind.Barrier: shield += p.Magnitude; break;
                    case SkillPrimitiveKind.Dash:
                    case SkillPrimitiveKind.Blink: dash += p.Magnitude; break;
                    case SkillPrimitiveKind.Leech: leech += p.Magnitude; break;
                    case SkillPrimitiveKind.Homing: break; // targeting aid; no direct numbers
                }
            }

            bool hasArea = area > 0f;
            int dotTicks = (dotPerTick > 0f) ? BaseDotTicks + dotDuration : 0;
            int hitCount = hasArea ? availableTargets : Math.Min(availableTargets, 1 + spread);

            var hits = new List<TargetEffect>(hitCount);
            float directTotal = 0f;
            for (int i = 0; i < hitCount; i++)
            {
                float dmg = 0f;
                if (i == 0) dmg += single;                       // primary takes the focused hit
                else if (i <= spread) dmg += single * SpreadFalloff; // chained/pierced
                if (hasArea) dmg += area;

                if (dmg <= 0f && dotPerTick <= 0f && control == ControlKind.None) continue;

                directTotal += dmg;
                hits.Add(new TargetEffect(i, dmg, dotPerTick, dotTicks, control));
            }

            float selfHeal = directTotal * (leech * LeechFractionPerMagnitude);
            return new SkillResolution(hits, selfHeal, shield * ShieldPerMagnitude, dash * DashPerMagnitude);
        }

        private static ControlKind Max(ControlKind a, ControlKind b) => (ControlKind)Math.Max((int)a, (int)b);
    }
}
