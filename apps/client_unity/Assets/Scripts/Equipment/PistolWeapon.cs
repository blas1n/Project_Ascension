using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>Firearm: instant hitscan raycast.</summary>
    public sealed class PistolWeapon : WeaponBase
    {
        protected override void OnPrimary(AttackContext ctx)
        {
            var dir = ctx.Direction.normalized;
            var origin = ctx.Origin + dir * 0.5f; // clear the attacker
            var end = ctx.Origin + dir * Data.Range;

            if (Physics.Raycast(origin, dir, out var hit, Data.Range))
            {
                end = hit.point;
                if (hit.collider.TryGetComponent<IDamageable>(out var target) && !target.IsDead)
                    target.TakeDamage(Data.Damage, ctx.Attacker);
            }

            CombatDebug.Tracer(ctx.Origin, end, Color.cyan);
        }
    }
}
