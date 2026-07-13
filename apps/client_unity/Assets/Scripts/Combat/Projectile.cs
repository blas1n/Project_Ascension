using UnityEngine;
using ProjectAscension.GameSimulation.Physics;
using NumVec3 = System.Numerics.Vector3;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// A projectile (arrow / spell bolt). Its trajectory AND its hit are both owned by the pure,
    /// deterministic <see cref="ProjectileSim"/> (ADR 0013) — the same code the server will one day
    /// run unchanged. This shell only describes the launch, steps the sim against
    /// <see cref="SimWorld.Collision"/> every frame, and renders whatever it decides: moves the
    /// transform while flying, plays the impact VFX and applies damage on a hit. Spawn via
    /// <see cref="ProjectileFactory"/>.
    ///
    /// ProjectileSim sweeps the segment the bolt actually travelled each step, arming by distance
    /// from the muzzle rather than reacting to a trigger. Trigger events used to cost us three bugs
    /// at once: a bolt that spawned overlapping something detonated on it; a frame hitch let the
    /// bolt jump metres and materialise INSIDE geometry, which read as "the shot vanished the
    /// instant I fired"; and a fast bolt could tunnel clean through a thin wall. A swept segment,
    /// armed by distance, has none of those failure modes.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private ProjectileSimState _state;
        private ProjectileSimSettings _settings;
        private float _damage;
        private GameObject _owner;
        private Color _impactColor = Color.white;

        /// <summary>Colour of the impact burst spawned when this bolt hits (set by the factory).</summary>
        public void SetImpactColor(Color color) => _impactColor = color;

        public void Launch(Vector3 direction, float speed, float damage, GameObject owner,
            float lifetime = 5f, float gravity = 0f, float radius = 0.06f)
        {
            var dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            transform.forward = dir;
            _damage = damage;
            _owner = owner;

            _state = new ProjectileSimState(ToNum(transform.position), ToNum(dir) * speed);
            _settings = new ProjectileSimSettings
            {
                Gravity = gravity,
                Radius = Mathf.Max(0.01f, radius),
                OwnerActorId = SimWorld.ActorIdOf(owner),
                Lifetime = lifetime,
            };
        }

        private void Update()
        {
            ProjectileOutcome outcome;
            (_state, outcome) = ProjectileSim.Step(_state, _settings, SimWorld.Collision, Time.deltaTime);

            switch (outcome.Status)
            {
                case ProjectileStatus.Hit:
                    Impact(ToUnity(outcome.Point), outcome.HitActorId);
                    break;
                case ProjectileStatus.Expired:
                    Destroy(gameObject);
                    break;
                default:
                    transform.position = ToUnity(_state.Position);
                    if (_state.Velocity.LengthSquared() > 0.0001f) transform.forward = ToUnity(_state.Velocity).normalized; // point along the arc
                    break;
            }
        }

        private static NumVec3 ToNum(Vector3 v) => new NumVec3(v.x, v.y, v.z);
        private static Vector3 ToUnity(NumVec3 v) => new Vector3(v.X, v.Y, v.Z);

        private void Impact(Vector3 point, int hitActorId)
        {
            transform.position = point;
            if (SimWorld.TryGetDamageable(hitActorId, out var target))
                target.TakeDamage(_damage, _owner);

            CombatVfx.Burst(point, _impactColor);
            Destroy(gameObject);
        }
    }
}
