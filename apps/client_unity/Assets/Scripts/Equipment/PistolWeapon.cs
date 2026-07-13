using UnityEngine;
using ProjectAscension.Combat;
using NumVec3 = System.Numerics.Vector3;

namespace ProjectAscension.Equipment
{
    /// <summary>Firearm: instant hitscan — a <see cref="SimWorld"/> sweep of radius 0 (ADR 0013),
    /// the sim's ray.</summary>
    public sealed class PistolWeapon : WeaponBase
    {
        protected override void OnPrimary(AttackContext ctx, float charge)
        {
            var dir = SpreadDirection(ctx.Direction.normalized); // deviate by sustained-fire bloom
            var origin = ctx.Origin + dir * 0.5f; // clear the attacker
            var end = ctx.Origin + dir * Data.Range;

            bool struck = false;
            int ownerActorId = SimWorld.ActorIdOf(ctx.Attacker);
            if (SimWorld.Collision.SweepSphere(ToNum(origin), ToNum(origin + dir * Data.Range), 0f, ownerActorId, out var hit))
            {
                end = ToUnity(hit.Point);
                struck = true;
                if (SimWorld.TryGetDamageable(hit.ActorId, out var target) && !target.IsDead)
                    target.TakeDamage(Data.Damage, ctx.Attacker);
            }

            var color = new Color(0.5f, 0.85f, 1f);
            CombatVfx.Tracer(ctx.Origin, end, color);
            if (struck) CombatVfx.Burst(end, color, 0.6f); // impact spark
        }

        private static NumVec3 ToNum(Vector3 v) => new NumVec3(v.x, v.y, v.z);
        private static Vector3 ToUnity(NumVec3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
