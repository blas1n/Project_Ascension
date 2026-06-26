using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Discovery;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class ComboRecognizerTests
    {
        private static DiscoveredSkill DodgeSlash() => new(
            "Dodge Slash", ManifestationKind.Command,
            new Skill("Dodge Slash", new[] { new SkillPrimitive(SkillPrimitiveKind.Dash, 1) }));

        private static ComboRecognizer WithDodgeSlash(out DiscoveredSkill skill, float window = 1.5f)
        {
            var recognizer = new ComboRecognizer(window);
            skill = DodgeSlash();
            recognizer.Register(new[] { BehaviorKind.Dodge, BehaviorKind.MeleeAttack }, skill);
            return recognizer;
        }

        [Fact]
        public void CompletingTheCombo_FiresTheCommand()
        {
            var recognizer = WithDodgeSlash(out var skill);

            Assert.Null(recognizer.Feed(BehaviorKind.Dodge, 0.0f));
            Assert.Same(skill, recognizer.Feed(BehaviorKind.MeleeAttack, 0.3f));
        }

        [Fact]
        public void WrongOrder_DoesNotFire()
        {
            var recognizer = WithDodgeSlash(out _);

            Assert.Null(recognizer.Feed(BehaviorKind.MeleeAttack, 0.0f));
            Assert.Null(recognizer.Feed(BehaviorKind.Dodge, 0.3f));
        }

        [Fact]
        public void OutsideTheWindow_DoesNotFire()
        {
            var recognizer = WithDodgeSlash(out _, window: 1.0f);

            Assert.Null(recognizer.Feed(BehaviorKind.Dodge, 0.0f));
            Assert.Null(recognizer.Feed(BehaviorKind.MeleeAttack, 2.0f)); // 2s > 1s window
        }

        [Fact]
        public void MatchesAsTail_IgnoringEarlierNoise()
        {
            var recognizer = WithDodgeSlash(out var skill);

            recognizer.Feed(BehaviorKind.Jump, 0.0f);
            Assert.Null(recognizer.Feed(BehaviorKind.Dodge, 0.2f));
            Assert.Same(skill, recognizer.Feed(BehaviorKind.MeleeAttack, 0.4f));
        }

        [Fact]
        public void ShortCombos_AreNotRegistered()
        {
            var recognizer = new ComboRecognizer();
            Assert.False(recognizer.Register(new[] { BehaviorKind.Jump }, DodgeSlash()));
        }

        [Fact]
        public void Builder_DropsDerivedAndDuplicates()
        {
            var combo = ComboBuilder.FromBehaviors(new[] { "Dodge", "MeleeAttack", "DodgeAttack", "Dodge" });

            Assert.Equal(new[] { BehaviorKind.Dodge, BehaviorKind.MeleeAttack }, combo);
        }
    }
}
