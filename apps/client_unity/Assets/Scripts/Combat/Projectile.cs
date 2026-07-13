using System;
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
    ///
    /// Hits are found by SWEEPING the segment the bolt actually travelled each step, not by
    /// waiting for a trigger to fire. Trigger events cost us three bugs at once: a bolt that
    /// spawned overlapping something died on it; a frame hitch let the bolt jump metres and
    /// materialise INSIDE geometry, which read as "the shot vanished the instant I fired";
    /// and a fast bolt could tunnel clean through a thin wall. A swept segment has none of
    /// those failure modes — it hits the first thing between where it was and where it is.
    /// </summary>
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
        private float _radius = 0.06f;
        private float _travelled;
        private Color _impactColor = Color.white;

        /// <summary>A bolt cannot be stopped by anything it is ALREADY inside of until it has cleared
        /// the muzzle. You fire from the eye, and the eye is inside the player — and sometimes inside
        /// the wall you are standing against.</summary>
        private const float ArmDistance = 0.5f;

        /// <summary>The most simulation one frame may catch up on. Without this, a hitch (a shader
        /// compile, a first-shot allocation) hands the bolt a 300 ms step and it teleports across the
        /// room in a single frame.</summary>
        private const float MaxCatchUp = 0.1f;

        /// <summary>Colour of the impact burst spawned when this bolt hits (set by the factory).</summary>
        public void SetImpactColor(Color color) => _impactColor = color;

        public void Launch(Vector3 direction, float speed, float damage, GameObject owner,
            float lifetime = 5f, float gravity = 0f, float radius = 0.06f)
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
            _radius = Mathf.Max(0.01f, radius);
            _travelled = 0f;
        }

        private void Update()
        {
            // Advance the trajectory in fixed deterministic steps (not raw frame time), so the arc is
            // identical regardless of framerate and reproducible on the server.
            _accumulator = Mathf.Min(_accumulator + Time.deltaTime, MaxCatchUp);
            while (_accumulator >= Ballistics.FixedStep)
            {
                var from = _position;
                (_position, _velocity) = Ballistics.Step(_position, _velocity, _gravity, Ballistics.FixedStep);
                _accumulator -= Ballistics.FixedStep;

                if (Sweep(ToUnity(from), ToUnity(_position))) return; // hit something on the way
            }

            transform.position = ToUnity(_position);
            if (_velocity.LengthSquared() > 0.0001f) transform.forward = ToUnity(_velocity).normalized; // point along the arc
            _age += Time.deltaTime;
            if (_age >= _lifetime)
                Destroy(gameObject);
        }

        /// <summary>Everything between where the bolt was and where it now is. True if the flight ended.</summary>
        private bool Sweep(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            float distance = delta.magnitude;
            _travelled += distance;
            if (distance <= 0.0001f) return false;

            var hits = Physics.SphereCastAll(from, _radius, delta / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return false;
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // nearest first — a bolt stops at the FIRST thing

            foreach (var hit in hits)
            {
                if (IsOwn(hit.collider)) continue; // you cannot shoot yourself
                // distance 0 means the sweep STARTED inside this collider: the muzzle is buried in it.
                // Not a hit until the bolt has cleared the barrel.
                if (hit.distance <= 0f && _travelled < ArmDistance) continue;

                var point = hit.distance <= 0f ? from : hit.point;
                if (hit.collider.TryGetComponent<IDamageable>(out var target))
                    target.TakeDamage(_damage, _owner);

                Impact(point);
                return true;
            }
            return false;
        }

        private bool IsOwn(Collider other)
            => _owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform));

        private static NumVec3 ToNum(Vector3 v) => new NumVec3(v.x, v.y, v.z);
        private static Vector3 ToUnity(NumVec3 v) => new Vector3(v.X, v.Y, v.Z);

        private void Impact(Vector3 point)
        {
            transform.position = point;
            CombatVfx.Burst(point, _impactColor);
            Destroy(gameObject);
        }
    }
}
