namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The focus a skill costs to cast — derived deterministically from its primitives'
    /// magnitude/range/duration, so a bigger composition costs more (matching its power).
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
    }
}
