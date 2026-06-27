using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>Arcane catalyst: casts a spell bolt. (Discovery integration later.)</summary>
    public sealed class CatalystWeapon : WeaponBase
    {
        protected override void OnPrimary(AttackContext ctx, float charge)
        {
            ProjectileFactory.Spawn(ctx, Data.ProjectileSpeed, Data.Damage, new Color(0.6f, 0.4f, 1f));
        }
    }
}
