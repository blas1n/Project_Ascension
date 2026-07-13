using System.Collections.Generic;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.GameSimulation.Monsters
{
    /// <summary>
    /// Deterministic monster placement (ADR: Unity is a shell) — WHERE a monster appears is a game
    /// fact, not a rendering detail, so it is sampled the same way spread and hit resolution are: a
    /// seeded, pure function instead of UnityEngine.Random. A server placing the same wave from the
    /// same seed gets the identical ring of positions.
    /// </summary>
    public static class SpawnPlacement
    {
        /// <summary>
        /// <paramref name="count"/> points scattered in an annulus around the origin — each at a
        /// uniformly random angle and a distance uniformly drawn between
        /// <paramref name="minDistance"/> and <paramref name="radius"/> (so nothing spawns on top of
        /// the spawner, and nothing spawns past the zone's edge). Ground-plane offsets (X, Z) — the
        /// caller supplies height and the spawner's own world position.
        /// </summary>
        public static IReadOnlyList<(float X, float Z)> Ring(uint seed, int count, float minDistance, float radius)
        {
            var points = new (float X, float Z)[count < 0 ? 0 : count];
            if (points.Length == 0) return points;

            float lo = minDistance < 0f ? 0f : minDistance;
            float hi = radius < lo ? lo : radius;

            var rng = new DeterministicRng(seed);
            for (int i = 0; i < points.Length; i++)
            {
                var (u1, afterAngle) = rng.NextFloat01();
                var (u2, afterDistance) = afterAngle.NextFloat01();
                rng = afterDistance;

                float angle = u1 * System.MathF.PI * 2f;
                float distance = lo + u2 * (hi - lo);
                points[i] = (System.MathF.Cos(angle) * distance, System.MathF.Sin(angle) * distance);
            }
            return points;
        }
    }
}
