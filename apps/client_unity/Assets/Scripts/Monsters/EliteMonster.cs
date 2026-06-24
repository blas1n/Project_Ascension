using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Monsters
{
    /// <summary>Tanky elite: fires a faster, harder-hitting bolt. (Boss is out of slice scope.)</summary>
    public sealed class EliteMonster : MonsterBase
    {
        protected override void PerformAttack()
        {
            if (Target == null) return;
            var muzzle = transform.position + Vector3.up * 1.2f;
            var aim = (Target.position + Vector3.up * 1f) - muzzle;
            var ctx = new AttackContext(muzzle, aim, gameObject);
            ProjectileFactory.Spawn(ctx, ProjectileSpeed, Damage, new Color(0.7f, 0.2f, 0.9f), 0.2f);
        }
    }
}
