using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Monsters
{
    /// <summary>Keeps distance and fires projectiles at the player.</summary>
    public sealed class RangedMonster : MonsterBase
    {
        protected override void PerformAttack()
        {
            if (Target == null) return;
            var muzzle = transform.position + Vector3.up * 1f;
            var aim = (Target.position + Vector3.up * 1f) - muzzle;
            var ctx = new AttackContext(muzzle, aim, gameObject);
            ProjectileFactory.Spawn(ctx, ProjectileSpeed, Damage, new Color(1f, 0.5f, 0.2f));
        }
    }
}
