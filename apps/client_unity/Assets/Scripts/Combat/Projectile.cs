using UnityEngine;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// A projectile (arrow / spell bolt). Travels along its velocity, optionally arcing
    /// under gravity (drop) — arrows arc, energy bolts fly straight. Applies damage to
    /// the first IDamageable it overlaps, then despawns. Spawn via
    /// <see cref="ProjectileFactory"/>.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _gravity;
        private float _damage;
        private float _lifetime;
        private GameObject _owner;
        private float _age;

        public void Launch(Vector3 direction, float speed, float damage, GameObject owner, float lifetime = 5f, float gravity = 0f)
        {
            var dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            transform.forward = dir;
            _velocity = dir * speed;
            _gravity = gravity;
            _damage = damage;
            _owner = owner;
            _lifetime = lifetime;
            _age = 0f;
        }

        private void Update()
        {
            if (_gravity > 0f) _velocity += Vector3.down * (_gravity * Time.deltaTime); // drop
            transform.position += _velocity * Time.deltaTime;
            if (_velocity.sqrMagnitude > 0.0001f) transform.forward = _velocity.normalized; // point along the arc
            _age += Time.deltaTime;
            if (_age >= _lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform)))
                return;

            if (other.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(_damage, _owner);
                Destroy(gameObject);
            }
            else if (!other.isTrigger)
            {
                Destroy(gameObject); // hit environment
            }
        }
    }
}
