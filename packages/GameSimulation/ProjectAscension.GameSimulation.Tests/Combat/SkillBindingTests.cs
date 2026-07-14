using System.Collections.Generic;
using System.Linq;
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
            var behaviours = new[] { "Use:melee", "Seq:jump>melee" };

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

        // --- BoundInstruments: the equipment GATE, not the ladder key --------------------------------
        //
        // Reproduces the live playtest bug ("장착 선택할 때 현재 발동 가능한거만 보여줘야 해. 지금은 전부
        // 보여주는 듯" — the hotkey binder showed commands regardless of what was equipped). Pulled
        // straight from the project's dev DB: "Converging Leap" (Ready, Command manifestation) had
        // BehaviorsJson exactly like the first case below — made by fusing a forged weapon
        // ("spell:aerial-synthesis") with jump. RequiredEquipment (the ladder key's vocabulary) is
        // EMPTY for it by design (ADR 0011 — a forged weapon's own tag must not become a ladder key,
        // or a player could farm a fresh easy ladder per discovery), so anything gated only by
        // RequiredEquipment reads this as a body skill and shows it for every loadout. But a weapon
        // DID take part — a forged one — so BoundInstruments (what the picker/CommandGate must use)
        // has to say so.

        [Fact]
        public void ATechniqueMadeThroughAForgedWeapon_RequiresThatWeaponEquipped()
        {
            // The real shape of "Converging Leap" in the dev DB (trimmed to the fields that matter).
            var behaviours = new[]
            {
                "Jump", "Use:jump", "While:jump@airborne",
                "RangedAttack", "Use:spell:aerial-synthesis", "While:spell:aerial-synthesis@airborne",
                "Seq:jump>spell:aerial-synthesis", "Seq:spell:aerial-synthesis>jump",
                "Fuse:jump>spell:aerial-synthesis", "Fuse:spell:aerial-synthesis>jump",
                "While:spell:aerial-synthesis@moving",
            };

            // The LADDER key must still see nothing bindable here (ADR 0011's anti-farming rule) —
            // unchanged from before this fix.
            Assert.Empty(SkillBinding.RequiredEquipment(behaviours));

            // But the USE GATE must see the forged weapon that actually took part — this is the fix.
            Assert.Equal(new[] { "spell:aerial-synthesis" }, SkillBinding.BoundInstruments(behaviours));
            Assert.False(SkillBinding.Usable(behaviours, new HashSet<string>())); // nothing equipped
            Assert.False(SkillBinding.Usable(behaviours, new HashSet<string> { "firearm" })); // wrong weapon
            Assert.True(SkillBinding.Usable(behaviours, new HashSet<string> { "spell:aerial-synthesis" }));
        }

        [Fact]
        public void AForgedWeaponFusedWithABaseCategory_BindsBoth()
        {
            var behaviours = new[] { "Use:firearm", "Fuse:spell:emberbrand>firearm", "Jump" };

            Assert.Equal(
                new HashSet<string> { "firearm", "spell:emberbrand" },
                SkillBinding.BoundInstruments(behaviours).ToHashSet());
            Assert.False(SkillBinding.Usable(behaviours, new HashSet<string> { "firearm" })); // half missing
            Assert.True(SkillBinding.Usable(behaviours, new HashSet<string> { "firearm", "spell:emberbrand" }));
        }

        [Fact]
        public void ASkillMadeWithOnlyTheBody_IsStillUnrestrictedUnderBoundInstruments()
        {
            var behaviours = new[] { "Jump", "Chain:jump", "Use:jump" };
            Assert.Empty(SkillBinding.BoundInstruments(behaviours));
            Assert.True(SkillBinding.Usable(behaviours, new HashSet<string>()));
        }
    }
}
