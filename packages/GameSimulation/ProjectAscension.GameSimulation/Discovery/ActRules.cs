namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>
    /// The predicates that decide an <see cref="Act"/>'s qualities (ADR: Unity is a shell) — the
    /// discovery grammar's inputs are game facts, not magic constants sitting in a MonoBehaviour.
    /// </summary>
    public static class ActRules
    {
        /// <summary>Whether a frame's horizontal displacement is real travel (the
        /// <see cref="ActQuality.Moving"/> quality), not a rounding twitch. The threshold is a distance
        /// in metres — DB-driven (<c>CombatTuning.MovingDistanceThreshold</c>) like every other balance
        /// number, not hard-coded here.</summary>
        public static bool IsMoving(float deltaX, float deltaZ, float thresholdMeters)
        {
            float thresholdSqr = thresholdMeters * thresholdMeters;
            return (deltaX * deltaX + deltaZ * deltaZ) > thresholdSqr;
        }
    }
}
