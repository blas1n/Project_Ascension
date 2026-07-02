using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>Firearm: instant hitscan raycast.</summary>
    public sealed class PistolWeapon : WeaponBase
    {
        protected override void OnPrimary(AttackContext ctx, float charge)
        {
            var dir = SpreadDirection(ctx.Direction.normalized); // deviate by sustained-fire bloom
            var origin = ctx.Origin + dir * 0.5f; // clear the attacker
            var end = ctx.Origin + dir * Data.Range;

            bool struck = false;
            if (Physics.Raycast(origin, dir, out var hit, Data.Range))
            {
                end = hit.point;
                struck = true;
                if (hit.collider.TryGetComponent<IDamageable>(out var target) && !target.IsDead)
                    target.TakeDamage(Data.Damage, ctx.Attacker);
            }

            var color = new Color(0.5f, 0.85f, 1f);
            CombatVfx.Tracer(ctx.Origin, end, color);
            if (struck) CombatVfx.Burst(end, color, 0.6f); // impact spark
        }
    }
}
