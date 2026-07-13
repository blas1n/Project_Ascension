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

        // Magazine state (ReloadRules — GameSimulation owns the gating, this just holds the numbers).
        // A magazine-less weapon (MagazineSize 0) never touches any of this: CanFire/CanBeginReload
        // both short-circuit true/false for it.
        private int _loaded;
        private bool _isReloading;
        private float _reloadStart = -1f;

        public WeaponData Data => _data;

        /// <summary>Charge (0..1) of the most recent shot — 0 for instant weapons. Lets
        /// the input layer announce a charged-attack fact for discovery.</summary>
        public float LastCharge { get; private set; }

        /// <summary>Rounds currently in the magazine. Meaningless (reads 0) for a weapon with no
        /// magazine — check <see cref="HasMagazine"/> before displaying it.</summary>
        public int Loaded => _loaded;

        /// <summary>The weapon's magazine capacity — 0 means it has no magazine at all.</summary>
        public int MagazineSize => _data != null ? _data.MagazineSize : 0;

        /// <summary>Whether this weapon has a magazine to begin with (sword/bow/catalyst/shield
        /// don't). The HUD uses this to decide whether to draw ammo at all.</summary>
        public bool HasMagazine => MagazineSize > 0;

        /// <summary>True while the weapon is mid-reload — cannot fire until it finishes.</summary>
        public bool IsReloading => _isReloading;

        /// <summary>Reload progress (0..1) for a HUD bar — 0 when not reloading.</summary>
        public float ReloadFraction =>
            ReloadRules.ReloadFraction(_isReloading, _reloadStart, Time.time, _data != null ? _data.ReloadTime : 0f);

        public void Configure(WeaponData data)
        {
            _data = data;
            _spread = Spread.From(data.SpreadMin, data.SpreadMax);
            _loaded = data.MagazineSize; // starts full
            _isReloading = false;
            _reloadStart = -1f;
        }

        private void Update()
        {
            // Recover accuracy over time when not firing (firearms only).
            if (_data != null && _data.HasSpread)
                _spread = SpreadRules.Recover(_spread, _data.SpreadRecovery, Time.deltaTime);

            if (_isReloading && ReloadRules.ReloadComplete(_reloadStart, Time.time, _data.ReloadTime))
            {
                _loaded = _data.MagazineSize;
                _isReloading = false;
            }
        }

        /// <summary>Begin reloading this weapon now — a no-op (per ReloadRules) if it has no
        /// magazine, is already reloading, or the magazine is already full. Public because it is
        /// triggered two ways: automatically on a dry trigger pull (<see cref="TryFire"/>), and
        /// manually on the player's Reload input (PlayerCombat, both hands).</summary>
        public void BeginReload()
        {
            if (_data == null || !ReloadRules.CanBeginReload(_data.MagazineSize, _loaded, _isReloading)) return;
            _isReloading = true;
            _reloadStart = Time.time;
            // Reload is an ACT like any other verb (ADR 0009) — the grammar composes whatever
            // follows it without this weapon needing to know discovery exists.
            GameplayEvents.RaiseReloaded(EquipmentTags.For(_data));
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
        /// a charge weapon (e.g. a bow) starts charging and fires on release. Virtual because not
        /// every off-hand piece attacks — a shield uses the HELD input to raise a block instead.</summary>
        public virtual bool PrimaryDown(AttackContext ctx)
        {
            if (_data == null) return false;
            if (_data.IsCharged) { _chargeStart = Time.time; return false; }
            return TryFire(ctx, 0f);
        }

        /// <summary>Primary input released. A charge weapon fires scaled by how long it
        /// was held; an instant weapon does nothing. Returns true if it fired.</summary>
        public virtual bool PrimaryUp(AttackContext ctx)
        {
            if (_data == null || !_data.IsCharged || _chargeStart < 0f) return false;
            float charge = WeaponFireRules.ChargeFraction(_chargeStart, Time.time, _data.ChargeTime);
            _chargeStart = -1f;
            return TryFire(ctx, charge);
        }

        private bool TryFire(AttackContext ctx, float charge)
        {
            // Cooldown gating is a GameSimulation rule (headless-tested), not enforced here.
            if (!WeaponFireRules.CanFire(Time.time, _nextReadyTime)) return false;
            if (!ReloadRules.CanFire(_data.MagazineSize, _loaded, _isReloading))
            {
                // A dry trigger pull starts the reload automatically (FPS convention) — the player
                // is never left clicking a dead trigger. No-ops harmlessly if already reloading.
                BeginReload();
                return false;
            }
            _nextReadyTime = WeaponFireRules.NextReady(Time.time, _data.Cooldown);
            LastCharge = charge;
            if (_data.HasSpread) _spread = SpreadRules.Bloom(_spread, _data.SpreadPerShot); // bloom on each shot
            if (HasMagazine) _loaded = ReloadRules.AfterShot(_loaded);
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
