using System;
using UnityEngine;

namespace ProjectAscension.Game
{
    /// <summary>
    /// A discovered skill's projectile delivery (DeliveryMotion.Projectile): travels per the
    /// DeliverySpec and, on hitting something or expiring, resolves the skill at the impact
    /// point via the supplied callback. It only carries the skill — the caster owns the
    /// effect (SkillResolver). Does its own linecast so it can't pass through walls/monsters.
    /// </summary>
    public sealed class SkillProjectile : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _gravity;
        private float _remaining; // seconds of flight left (range / speed)
        private LayerMask _mask;
        private Action<Vector3> _onImpact;
        private bool _spent;

        public void Launch(Vector3 position, Vector3 direction, float speed, float gravity, float range, LayerMask mask, Action<Vector3> onImpact)
        {
            transform.position = position;
            _velocity = direction.normalized * speed;
            _gravity = gravity;
            _remaining = speed > 0.01f ? range / speed : 0.01f;
            _mask = mask;
            _onImpact = onImpact;
        }

        private void Update()
        {
            if (_spent) return;
            float dt = Time.deltaTime;
            var next = transform.position + _velocity * dt;
            if (Physics.Linecast(transform.position, next, out var hit, _mask, QueryTriggerInteraction.Ignore))
            {
                Impact(hit.point);
                return;
            }
            transform.position = next;
            _velocity += Vector3.down * _gravity * dt;
            _remaining -= dt;
            if (_remaining <= 0f) Impact(transform.position);
        }

        private void Impact(Vector3 point)
        {
            _spent = true;
            _onImpact?.Invoke(point);
            Destroy(gameObject);
        }
    }
}
