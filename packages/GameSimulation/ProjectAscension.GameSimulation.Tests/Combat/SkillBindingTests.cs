using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>
    /// What a discovered skill belongs to (ADR 0011). The properties that matter: a skill made through a
    /// weapon is that weapon's, a skill made with the body is yours — and only the weapons that actually
    /// TOOK PART are owed anything. What you were merely carrying is not evidence.
    /// </summary>
    public class SkillBindingTests
    {
        private static readonly string[] Both = { "firearm", "arcane" };
        private static readonly string[] GunOnly = { "firearm" };
        private static readonly string[] Nothing = new string[0];

        [Fact]
        public void AFusionOfTwoHands_BindsBOTH()
        {
            var required = SkillBinding.RequiredEquipment(new[] { "Fuse:arcane>firearm", "RangedAttack" });

            Assert.Contains("arcane", required);
            Assert.Contains("firearm", required);
            Assert.True(SkillBinding.Usable(new[] { "Fuse:arcane>firearm" }, Both));
            Assert.False(SkillBinding.Usable(new[] { "Fuse:arcane>firearm" }, GunOnly)); // half the skill is missing
        }

        [Fact]
        public void CarryingACatalystWhileYouShoot_DoesNotMakeItComplicit()
        {
            // THE correction: the old gate demanded everything you happened to be HOLDING. But if you
            // never wove the catalyst in, the skill is the gun's alone — and putting the catalyst away
            // must not take it from you.
            var behaviours = new[] { "Use:firearm", "Chain:firearm", "RangedAttack" };
            var required = SkillBinding.RequiredEquipment(behaviours);

            Assert.Equal(new[] { "firearm" }, required);
            Assert.True(SkillBinding.Usable(behaviours, GunOnly));   // the catalyst is gone; the skill is not
            Assert.True(SkillBinding.Usable(behaviours, Both));
        }

        [Fact]
        public void ASkillMadeWithTheBody_IsYoursToKeep()
        {
            // Double-jumping was learned by jumping, not by shooting. Sheathing your sword does not
            // unteach it.
            var behaviours = new[] { "Jump", "Chain:jump", "Use:jump" };

            Assert.Empty(SkillBinding.RequiredEquipment(behaviours));
            Assert.True(SkillBinding.Usable(behaviours, Nothing));
            Assert.True(SkillBinding.Usable(behaviours, null));
        }

        [Fact]
        public void ASkillMadeThroughTheWeapon_IsGoneWithoutIt()
        {
            var behaviours = new[] { "Use:melee", "Seq:dodge>melee" };

            Assert.Equal(new[] { "melee" }, SkillBinding.RequiredEquipment(behaviours));
            Assert.False(SkillBinding.Usable(behaviours, GunOnly)); // a swordsman's technique means nothing to a gunman
            Assert.True(SkillBinding.Usable(behaviours, new[] { "melee" }));
        }

        [Fact]
        public void ADiscoveredWeaponsOwnTag_NeverBinds()
        {
            // Otherwise a skill would be bound to the skill that made it, and the loop would eat itself.
            Assert.Empty(SkillBinding.RequiredEquipment(new[] { "Use:spell:emberbrand", "Jump" }));
        }

        [Fact]
        public void AWeaponIsImplicatedOnlyWhereTheGrammarNamedIt()
        {
            // Token-boundary matching: a behaviour that merely contains the letters must not implicate a
            // weapon that never took part.
            Assert.Empty(SkillBinding.RequiredEquipment(new[] { "Chain:meleeless", "Use:firearmish" }));
        }
    }
}
