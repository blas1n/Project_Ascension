namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The focus a skill costs to cast — derived deterministically from its primitives'
    /// magnitude/range/duration, so a bigger composition costs more (matching its power).
    /// </summary>
    public static class FocusCost
    {
        public const float PerPoint = 4f;

        public static float Of(Skill skill)
        {
            float points = 0f;
            foreach (var p in skill.Primitives)
                points += p.Magnitude + p.Range + p.Duration;
            return points * PerPoint;
        }
    }
}
