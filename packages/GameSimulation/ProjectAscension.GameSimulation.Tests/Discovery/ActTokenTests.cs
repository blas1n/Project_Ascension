using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Discovery;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Discovery
{
    /// <summary>
    /// How an act names itself (ADR 0009). This is the sharpest edge in the whole discovery engine: the
    /// token is what the grammar fuses, so two acts that share a token are, to the engine, the same act.
    /// </summary>
    public class ActTokenTests
    {
        [Fact]
        public void AnAttack_NamesItsWeaponAndNothingElse()
        {
            // "Fuse:arcane>firearm" has to read "a spell woven into a GUNSHOT".
            Assert.Equal("firearm", new Act("attack", "firearm", 1f).Token);
        }

        [Fact]
        public void AReloadCannotPassItselfOffAsAShot()
        {
            // The bug this pins: with Token = instrument, reloading a gun and FIRING it collapsed to the
            // same token — so casting a spell next to a RELOAD scored as a spell fused into a shot, and
            // handed out a flame bullet nobody earned.
            var shot = new Act("attack", "firearm", 1f);
            var reload = new Act("reload", "firearm", 1f);

            Assert.NotEqual(shot.Token, reload.Token);
            Assert.Equal("reload:firearm", reload.Token);
        }

        [Fact]
        public void AHandledWeaponIsStillBoundToTheSkillItMade()
        {
            // ...but the weapon must stay IN the name, or a skill made by weaving a spell into a reload
            // would not belong to the gun (ADR 0011) and you could use it bare-handed.
            var behaviors = new[] { "Fuse:arcane>reload:firearm", "Use:reload:firearm" };

            var required = SkillBinding.RequiredEquipment(behaviors);

            Assert.Contains("firearm", required);
            Assert.Contains("arcane", required);
            Assert.False(SkillBinding.Usable(behaviors, new[] { "arcane" })); // no gun, no skill
        }

        [Fact]
        public void AVerbWithNoInstrument_IsJustItself()
        {
            Assert.Equal("jump", new Act("jump", null, 1f).Token);
        }
    }
}
