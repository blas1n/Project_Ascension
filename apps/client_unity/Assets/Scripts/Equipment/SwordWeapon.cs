using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>Melee weapon: a short overlap swing in front of the attacker.</summary>
    public sealed class SwordWeapon : WeaponBase
    {
        private const float SwingRadius = 1.1f;

        protected override void OnPrimary(AttackContext ctx, float charge)
        {
            var center = ctx.Origin + ctx.Direction.normalized * Data.Range;
            var hits = Physics.OverlapSphere(center, SwingRadius);
            foreach (var hit in hits)
            {
                if (ctx.Attacker != null && hit.transform.IsChildOf(ctx.Attacker.transform)) continue;
                if (hit.TryGetComponent<IDamageable>(out var target) && !target.IsDead)
                    target.TakeDamage(Data.Damage, ctx.Attacker);
            }

            CombatVfx.Burst(center, new Color(1f, 0.45f, 0.3f), SwingRadius); // swing arc
        }
    }
}
