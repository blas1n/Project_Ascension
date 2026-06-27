using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Combat;

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
        private Spread _spread;

        public WeaponData Data => _data;

        /// <summary>Charge (0..1) of the most recent shot — 0 for instant weapons. Lets
        /// the input layer announce a charged-attack fact for discovery.</summary>
        public float LastCharge { get; private set; }

        public void Configure(WeaponData data)
        {
            _data = data;
            _spread = Spread.From(data.SpreadMin, data.SpreadMax);
        }

        private void Update()
        {
            // Recover accuracy over time when not firing (firearms only).
            if (_data != null && _data.HasSpread)
                _spread = SpreadRules.Recover(_spread, _data.SpreadRecovery, Time.deltaTime);
        }

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
            LastCharge = charge;
            if (_data.HasSpread) _spread = SpreadRules.Bloom(_spread, _data.SpreadPerShot); // bloom on each shot
            OnPrimary(ctx, charge);
            return true;
        }

        /// <summary>Deviate an aim direction by the current spread cone (a no-op for
        /// precise weapons). Firing subclasses use this for their shot direction.</summary>
        protected Vector3 SpreadDirection(Vector3 direction)
        {
            if (_data == null || !_data.HasSpread || _spread.Current <= 0f) return direction;
            float a = _spread.Current;
            var deviation = Quaternion.Euler(Random.Range(-a, a), Random.Range(-a, a), 0f);
            return (Quaternion.LookRotation(direction) * deviation * Vector3.forward).normalized;
        }

        /// <summary>Execute the attack. <paramref name="charge"/> is 0..1 (0 for instant
        /// weapons); charge weapons scale damage/speed with it.</summary>
        protected abstract void OnPrimary(AttackContext ctx, float charge);

        /// <summary>Secondary use (aim/block). No-op until later.</summary>
        public virtual void SecondaryAction(AttackContext ctx) { }
    }
}
