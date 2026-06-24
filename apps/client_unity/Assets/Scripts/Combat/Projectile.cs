using UnityEngine;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// Straight-line projectile (arrow / spell bolt). Applies damage to the first
    /// IDamageable it overlaps, then despawns. Spawn via <see cref="ProjectileFactory"/>.
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        private float _speed;
        private float _damage;
        private float _lifetime;
        private GameObject _owner;
        private float _age;

        public void Launch(Vector3 direction, float speed, float damage, GameObject owner, float lifetime = 5f)
        {
            transform.forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            _speed = speed;
            _damage = damage;
            _owner = owner;
            _lifetime = lifetime;
            _age = 0f;
        }

        private void Update()
        {
            transform.position += transform.forward * (_speed * Time.deltaTime);
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
