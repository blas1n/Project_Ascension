using UnityEngine;
using ProjectAscension.GameSimulation.Combat;
using NumVec3 = System.Numerics.Vector3;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// A projectile (arrow / spell bolt). Its trajectory — the arc under gravity — is
    /// advanced by the framerate-independent <see cref="Ballistics"/> core (a fixed sim
    /// tick, not Unity frame time). In the MMO the server owns this and replicates the
    /// path; this client copy is a view/prediction (ADR 0006). Unity owns spatial hit
    /// detection; the damage outcome resolves through the core. Spawn via
    /// <see cref="ProjectileFactory"/>.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        private NumVec3 _position;
        private NumVec3 _velocity;
        private float _gravity;
        private float _accumulator;
        private float _damage;
        private float _lifetime;
        private GameObject _owner;
        private float _age;
        private Color _impactColor = Color.white;

        // The bolt can't hit anything for this long after launch, so it can't self-destruct
        // on a collider it spawns overlapping (the muzzle, the ground under the camera, the
        // caster) before it has cleared the barrel — which read as "the shot never appears".
        private const float ArmDelay = 0.05f;

        /// <summary>Colour of the impact burst spawned when this bolt hits (set by the factory).</summary>
        public void SetImpactColor(Color color) => _impactColor = color;

        public void Launch(Vector3 direction, float speed, float damage, GameObject owner, float lifetime = 5f, float gravity = 0f)
        {
            var dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            transform.forward = dir;
            _position = ToNum(transform.position);
            _velocity = ToNum(dir) * speed;
            _gravity = gravity;
            _accumulator = 0f;
            _damage = damage;
            _owner = owner;
            _lifetime = lifetime;
            _age = 0f;
        }

        private void Update()
        {
            // Advance the trajectory in fixed deterministic steps (not raw frame time),
            // so the arc is identical regardless of framerate and reproducible on the server.
            _accumulator += Time.deltaTime;
            while (_accumulator >= Ballistics.FixedStep)
            {
                (_position, _velocity) = Ballistics.Step(_position, _velocity, _gravity, Ballistics.FixedStep);
                _accumulator -= Ballistics.FixedStep;
            }

            transform.position = ToUnity(_position);
            if (_velocity.LengthSquared() > 0.0001f) transform.forward = ToUnity(_velocity).normalized; // point along the arc
            _age += Time.deltaTime;
            if (_age >= _lifetime)
                Destroy(gameObject);
        }

        private static NumVec3 ToNum(Vector3 v) => new NumVec3(v.x, v.y, v.z);
        private static Vector3 ToUnity(NumVec3 v) => new Vector3(v.X, v.Y, v.Z);

        private void OnTriggerEnter(Collider other)
        {
            if (_age < ArmDelay) return; // still clearing the muzzle — ignore spawn overlaps
            if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform)))
                return;

            if (other.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(_damage, _owner);
                Impact();
            }
            else if (!other.isTrigger)
            {
                Impact(); // hit environment
            }
        }

        private void Impact()
        {
            CombatVfx.Burst(transform.position, _impactColor);
            Destroy(gameObject);
        }
    }
}
