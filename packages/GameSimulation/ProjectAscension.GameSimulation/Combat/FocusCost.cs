using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The focus a skill costs to cast — derived deterministically from its size, so a bigger
    /// composition costs more (matching its power). From the effect GRAPH's power points
    /// (ADR 0007 Phase 4c) when it has one, else its primitives' magnitude/range/duration.
    /// </summary>
    public static class FocusCost
    {
        // Cost-per-point comes from CombatTuning (DB-driven); Default mirrors the seed.
        public static float Of(Skill skill, CombatTuning tuning = null)
        {
            float points = 0f;
            foreach (var p in skill.Primitives)
                points += p.Magnitude + p.Range + p.Duration;
            return points * (tuning ?? CombatTuning.Default).FocusCostPerPoint;
        }

        /// <summary>Focus cost from the skill's effect graph (the graph analogue of the above).</summary>
        public static float Of(EffectNode graph, CombatTuning tuning = null)
            => EffectGraphQuery.PowerPoints(graph) * (tuning ?? CombatTuning.Default).FocusCostPerPoint;
    }
}
