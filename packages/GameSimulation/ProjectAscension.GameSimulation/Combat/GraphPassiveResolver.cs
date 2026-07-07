using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Resolves a passive skill's continuous bonuses from its effect GRAPH (ADR 0007 Phase 4c) —
    /// the graph analogue of <see cref="PassiveResolver"/>. A Ward under the trigger maps to the
    /// same always-on effect the defensive primitives did: Shield/Barrier reduce incoming damage,
    /// Leech returns health on damage dealt. Same <see cref="CombatTuning"/> weights (DB-driven),
    /// tier+1 standing in for magnitude, so the numbers match the primitive path. Movement/offense
    /// don't contribute here (that's MovementCapability / GraphSkillResolver).
    /// </summary>
    public static class GraphPassiveResolver
    {
        public static PassiveEffect Resolve(EffectNode graph, CombatTuning tuning = null)
        {
            var t = tuning ?? CombatTuning.Default;
            float reduction = 0f, lifesteal = 0f;
            Accumulate(graph, t, ref reduction, ref lifesteal);

            return new PassiveEffect(
                System.Math.Min(PassiveEffect.MaxDamageReduction, reduction),
                System.Math.Min(PassiveEffect.MaxLifesteal, lifesteal));
        }

        private static void Accumulate(EffectNode node, CombatTuning t, ref float reduction, ref float lifesteal)
        {
            switch (node)
            {
                case Trigger tr: Accumulate(tr.Child, t, ref reduction, ref lifesteal); break;
                case Sequence s:
                    foreach (var step in s.Steps) Accumulate(step, t, ref reduction, ref lifesteal);
                    break;
                case Ward w:
                    int mag = w.Tier + 1;
                    switch (w.Effect)
                    {
                        case WardEffect.Shield: reduction += mag * t.PassiveShieldReduction; break;
                        case WardEffect.Barrier: reduction += mag * t.PassiveBarrierReduction; break;
                        case WardEffect.Leech: lifesteal += mag * t.PassiveLeech; break;
                            // Heal is an on-cast burst (GraphSkillResolver), not an always-on passive.
                    }
                    break;
            }
        }
    }
}
