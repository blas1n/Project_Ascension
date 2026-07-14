using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// A shield: off-hand, and it never attacks. It defends only while the hand is HELD down — press to
    /// raise, release to lower. That is the FPS grammar the design calls for (art-direction:
    /// "방패 + 검 → 방어 → 근접 공격 → 반격"): defending is a decision you make and pay for, not a stat
    /// you carry. Whether a blow is actually stopped — and how much it absorbs — is the sim's rule
    /// (GameSimulation BlockRules, which also refuses to cover your flank); this only reports the raise.
    /// </summary>
    public sealed class ShieldWeapon : WeaponBase
    {
        /// <summary>True while the off hand is held down — the shield is up. Read by the player's
        /// HitReceiver, which asks BlockRules what that means for an incoming blow.</summary>
        public bool IsBlocking { get; private set; }

        // Returning false is deliberate: raising a shield is not an attack, so it must not announce one
        // (no Attacked fact → no attack-driven discovery signal, no swing). For the same reason a
        // shield ignores otherHandReloading entirely: the both-hands reload lock (ReloadRules.CanAttack)
        // only ever gates an actual attack (WeaponBase.TryFire), which this never calls — blocking still
        // works while the other hand reloads.
        public override bool PrimaryDown(AttackContext ctx, bool otherHandReloading)
        {
            IsBlocking = true;
            return false;
        }

        public override bool PrimaryUp(AttackContext ctx, bool otherHandReloading)
        {
            IsBlocking = false;
            return false;
        }

        /// <summary>Dropping the shield (swap/unequip) must never leave the block latched on.</summary>
        public override void OnUnequip()
        {
            IsBlocking = false;
            base.OnUnequip();
        }

        protected override void OnPrimary(AttackContext ctx, float charge) { } // a shield fires nothing
    }
}
