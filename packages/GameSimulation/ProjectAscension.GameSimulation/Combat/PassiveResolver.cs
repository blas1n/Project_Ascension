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
        private const float ShieldReductionPerMagnitude = 0.06f;
        private const float BarrierReductionPerMagnitude = 0.08f;
        private const float LeechPerMagnitude = 0.05f;

        public static PassiveEffect Resolve(Skill skill)
        {
            float reduction = 0f;
            float lifesteal = 0f;
            foreach (var p in skill.Primitives)
            {
                switch (p.Kind)
                {
                    case SkillPrimitiveKind.Shield: reduction += p.Magnitude * ShieldReductionPerMagnitude; break;
                    case SkillPrimitiveKind.Barrier: reduction += p.Magnitude * BarrierReductionPerMagnitude; break;
                    case SkillPrimitiveKind.Leech: lifesteal += p.Magnitude * LeechPerMagnitude; break;
                }
            }

            return new PassiveEffect(
                Math.Min(PassiveEffect.MaxDamageReduction, reduction),
                Math.Min(PassiveEffect.MaxLifesteal, lifesteal));
        }
    }
}
