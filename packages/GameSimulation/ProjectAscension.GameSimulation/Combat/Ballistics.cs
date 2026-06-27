using System.Numerics;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Deterministic projectile motion — the authoritative core for a projectile's
    /// trajectory (arc under gravity), so server and client compute the SAME path from
    /// the same launch. Advanced in a <see cref="FixedStep"/> increment (not frame time)
    /// so the arc is framerate-independent. Spatial hit detection stays in the renderer
    /// (it owns the geometry), but it queries along this deterministic trajectory and
    /// resolves the outcome through <see cref="CombatResolver"/>. Uses
    /// System.Numerics.Vector3 so it has no engine dependency.
    /// </summary>
    public static class Ballistics
    {
        /// <summary>The fixed integration step — the simulation tick, independent of the
        /// render framerate.</summary>
        public const float FixedStep = 1f / 60f;

        public static (Vector3 Position, Vector3 Velocity) Step(Vector3 position, Vector3 velocity, float gravity, float dt)
        {
            velocity -= new Vector3(0f, gravity, 0f) * dt; // gravity pulls down
            position += velocity * dt;
            return (position, velocity);
        }
    }
}
