using System;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Turns a passive discovery's primitives into its continuous <see cref="PassiveEffect"/>.
    /// The defensive primitives map to always-on bonuses: Shield/Barrier reduce incoming
    /// damage, Leech returns health on damage dealt. Deterministic, with fixed weights so
    /// the result is reproducible and balance-tunable.
    /// </summary>
    public static class PassiveResolver
    {
        // Conversions come from CombatTuning (DB-driven); Default mirrors the seeded values.
        public static PassiveEffect Resolve(Skill skill, CombatTuning tuning = null)
        {
            var t = tuning ?? CombatTuning.Default;
            float reduction = 0f;
            float lifesteal = 0f;
            int extraJumps = 0;
            foreach (var p in skill.Primitives)
            {
                switch (p.Kind)
                {
                    case SkillPrimitiveKind.Shield: reduction += p.Magnitude * t.PassiveShieldReduction; break;
                    case SkillPrimitiveKind.Barrier: reduction += p.Magnitude * t.PassiveBarrierReduction; break;
                    case SkillPrimitiveKind.Leech: lifesteal += p.Magnitude * t.PassiveLeech; break;
                    // Mobility → an extra air jump each (double jump); capped in the aggregate.
                    case SkillPrimitiveKind.Dash: extraJumps += 1; break;
                    case SkillPrimitiveKind.Blink: extraJumps += 1; break;
                }
            }

            return new PassiveEffect(
                Math.Min(PassiveEffect.MaxDamageReduction, reduction),
                Math.Min(PassiveEffect.MaxLifesteal, lifesteal),
                Math.Min(PassiveEffect.MaxExtraJumps, extraJumps));
        }
    }
}
