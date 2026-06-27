using System.Numerics;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The authoritative core for a projectile's trajectory (arc under gravity). The
    /// SERVER runs this to own where the projectile is; clients render the replicated
    /// result (ADR 0006 — server-authoritative replication, not lockstep, so a client's
    /// local copy is a correctable view, not a bit-exact re-simulation). Advanced in a
    /// <see cref="FixedStep"/> increment (the sim tick, not frame time) so the arc is
    /// framerate-independent. Spatial hit detection stays in the renderer (it owns the
    /// geometry, ADR 0004) but queries along this trajectory; the outcome resolves
    /// through <see cref="CombatResolver"/>. Uses System.Numerics.Vector3 — no engine
    /// dependency.
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
