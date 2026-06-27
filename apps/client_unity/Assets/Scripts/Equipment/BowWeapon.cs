using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// Ranged charge weapon (two-handed): hold to draw, release to loose an arrow whose
    /// damage and speed scale with the draw (ChargeRules). A snap shot still fires at
    /// the base multiplier.
    /// </summary>
    public sealed class BowWeapon : WeaponBase
    {
        protected override void OnPrimary(AttackContext ctx, float charge)
        {
            float mult = ChargeRules.Multiplier(charge, Data.MaxChargeMultiplier);
            var color = Color.Lerp(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.4f, 0.1f), charge);
            ProjectileFactory.Spawn(ctx, Data.ProjectileSpeed * mult, Data.Damage * mult, color);
        }
    }
}
