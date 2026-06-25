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

        /// <summary>Primary use, gated by cooldown. Returns true if it actually fired.</summary>
        public bool PrimaryAction(AttackContext ctx)
        {
            if (_data == null || Time.time < _nextReadyTime) return false;
            _nextReadyTime = Time.time + _data.Cooldown;
            OnPrimary(ctx);
            return true;
        }

        protected abstract void OnPrimary(AttackContext ctx);

        /// <summary>Secondary use (aim/block/charge). No-op until later.</summary>
        public virtual void SecondaryAction(AttackContext ctx) { }
    }
}
