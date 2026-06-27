using ProjectAscension.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// A discovered weapon (synthesized magic, ADR 0005): firing it casts the skill it
    /// was discovered as. The actual resolution/effects run in the Game-layer skill
    /// executor, so the weapon just announces the request (GameplayEvents); PlayerCombat
    /// already raises the attack fact that feeds further discovery.
    /// </summary>
    public sealed class SpellWeapon : WeaponBase
    {
        protected override void OnPrimary(AttackContext ctx)
        {
            if (Data != null && Data.DiscoveredSkill != null)
                GameplayEvents.RaiseSkillCastRequested(Data.DiscoveredSkill);
        }
    }
}
