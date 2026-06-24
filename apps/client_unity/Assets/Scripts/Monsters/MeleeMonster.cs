namespace ProjectAscension.Monsters
{
    /// <summary>Rushes the player and strikes in melee range.</summary>
    public sealed class MeleeMonster : MonsterBase
    {
        protected override void PerformAttack()
        {
            TargetDamageable?.TakeDamage(Damage, gameObject);
        }
    }
}
