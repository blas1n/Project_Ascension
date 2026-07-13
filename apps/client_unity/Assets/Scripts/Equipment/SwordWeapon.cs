using UnityEngine;
using ProjectAscension.Combat;
using NumVec3 = System.Numerics.Vector3;

namespace ProjectAscension.Equipment
{
    /// <summary>Melee weapon: a short overlap swing in front of the attacker — a
    /// <see cref="SimWorld"/> overlap (ADR 0013).</summary>
    public sealed class SwordWeapon : WeaponBase
    {
        private const float SwingRadius = 1.1f;

        protected override void OnPrimary(AttackContext ctx, float charge)
        {
            var center = ctx.Origin + ctx.Direction.normalized * Data.Range;
            int ownerActorId = SimWorld.ActorIdOf(ctx.Attacker); // excludes the attacker's own body outright
            foreach (var actorId in SimWorld.Collision.OverlapSphere(ToNum(center), SwingRadius, ownerActorId))
            {
                if (SimWorld.TryGetDamageable(actorId, out var target) && !target.IsDead)
                    target.TakeDamage(Data.Damage, ctx.Attacker);
            }

            CombatVfx.Burst(center, new Color(1f, 0.45f, 0.3f), SwingRadius); // swing arc
        }

        private static NumVec3 ToNum(Vector3 v) => new NumVec3(v.x, v.y, v.z);
    }
}
