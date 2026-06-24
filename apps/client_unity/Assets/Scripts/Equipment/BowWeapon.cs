using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>Ranged weapon: launches an arrow projectile.</summary>
    public sealed class BowWeapon : WeaponBase
    {
        protected override void OnPrimary(AttackContext ctx)
        {
            ProjectileFactory.Spawn(ctx, Data.ProjectileSpeed, Data.Damage, new Color(1f, 0.85f, 0.3f));
        }
    }
}
