using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// Common behaviour for an equippable weapon: equip/unequip + a cooldown-gated
    /// primary attack. Subclasses implement the attack (melee/hitscan/projectile).
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour, IEquippable
    {
        private WeaponData _data;
        private float _nextReadyTime;
        private float _chargeStart = -1f;

        public WeaponData Data => _data;

        public void Configure(WeaponData data) => _data = data;

        public virtual void OnEquip(Transform handAnchor)
        {
            transform.SetParent(handAnchor, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            gameObject.SetActive(true);
        }

        public virtual void OnUnequip() => gameObject.SetActive(false);

        /// <summary>Primary input pressed. An instant weapon fires now (returns true);
        /// a charge weapon (e.g. a bow) starts charging and fires on release.</summary>
        public bool PrimaryDown(AttackContext ctx)
        {
            if (_data == null) return false;
            if (_data.IsCharged) { _chargeStart = Time.time; return false; }
            return TryFire(ctx, 0f);
        }

        /// <summary>Primary input released. A charge weapon fires scaled by how long it
        /// was held; an instant weapon does nothing. Returns true if it fired.</summary>
        public bool PrimaryUp(AttackContext ctx)
        {
            if (_data == null || !_data.IsCharged || _chargeStart < 0f) return false;
            float charge = Mathf.Clamp01((Time.time - _chargeStart) / Mathf.Max(0.01f, _data.ChargeTime));
            _chargeStart = -1f;
            return TryFire(ctx, charge);
        }

        private bool TryFire(AttackContext ctx, float charge)
        {
            if (Time.time < _nextReadyTime) return false;
            _nextReadyTime = Time.time + _data.Cooldown;
            OnPrimary(ctx, charge);
            return true;
        }

        /// <summary>Execute the attack. <paramref name="charge"/> is 0..1 (0 for instant
        /// weapons); charge weapons scale damage/speed with it.</summary>
        protected abstract void OnPrimary(AttackContext ctx, float charge);

        /// <summary>Secondary use (aim/block). No-op until later.</summary>
        public virtual void SecondaryAction(AttackContext ctx) { }
    }
}
