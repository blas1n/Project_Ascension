using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class ComboRecognizerTests
    {
        private static DiscoveredSkill Command() => new(
            "Phase Step", ManifestationKind.Command,
            new Skill("Phase Step", new[] { new SkillPrimitive(SkillPrimitiveKind.Dash, 1) }));

        // Jump → RightClick → LeftClick, the engine-assigned combo.
        private static ComboRecognizer WithCommand(out DiscoveredSkill skill, float window = 1.5f)
        {
            var recognizer = new ComboRecognizer(window);
            skill = Command();
            recognizer.Register(new[] { InputToken.Jump, InputToken.RightClick, InputToken.LeftClick }, skill);
            return recognizer;
        }

        [Fact]
        public void CompletingTheCombo_FiresTheCommand()
        {
            var recognizer = WithCommand(out var skill);

            Assert.Null(recognizer.Feed(InputToken.Jump, 0.0f));
            Assert.Null(recognizer.Feed(InputToken.RightClick, 0.2f));
            Assert.Same(skill, recognizer.Feed(InputToken.LeftClick, 0.4f));
        }

        [Fact]
        public void WrongOrder_DoesNotFire()
        {
            var recognizer = WithCommand(out _);

            Assert.Null(recognizer.Feed(InputToken.Jump, 0.0f));
            Assert.Null(recognizer.Feed(InputToken.LeftClick, 0.2f));
            Assert.Null(recognizer.Feed(InputToken.RightClick, 0.4f));
        }

        [Fact]
        public void GapLongerThanWindow_BreaksTheChain()
        {
            var recognizer = WithCommand(out _, window: 1.0f);

            Assert.Null(recognizer.Feed(InputToken.Jump, 0.0f));
            Assert.Null(recognizer.Feed(InputToken.RightClick, 0.5f));
            Assert.Null(recognizer.Feed(InputToken.LeftClick, 1.7f)); // gap 1.2s > 1.0s → chain broke
        }

        [Fact]
        public void PerGapTiming_ForgivesALongTotalSpan()
        {
            var recognizer = WithCommand(out var skill, window: 1.5f);

            // Total span 2.0s > window, but each gap is only 1.0s ≤ window — still fires.
            Assert.Null(recognizer.Feed(InputToken.Jump, 0.0f));
            Assert.Null(recognizer.Feed(InputToken.RightClick, 1.0f));
            Assert.Same(skill, recognizer.Feed(InputToken.LeftClick, 2.0f));
        }

        // Combos are assigned prefix-free (ComboAssigner), but two can still share a SUFFIX;
        // the longest tail match wins, fired immediately.
        [Fact]
        public void LongestTailMatch_Wins()
        {
            var r = new ComboRecognizer(window: 1.5f);
            var two = Command();
            var three = Command();
            r.Register(new[] { InputToken.Jump, InputToken.RightClick }, two);          // suffix of the next
            r.Register(new[] { InputToken.Dodge, InputToken.Jump, InputToken.RightClick }, three);

            Assert.Null(r.Feed(InputToken.Dodge, 0.0f));
            Assert.Null(r.Feed(InputToken.Jump, 0.2f));
            // Tail is now both [Jump,RightClick] and [Dodge,Jump,RightClick] — the longer fires.
            Assert.Same(three, r.Feed(InputToken.RightClick, 0.4f));
        }

        [Fact]
        public void MatchesAsTail_IgnoringEarlierNoise()
        {
            var recognizer = WithCommand(out var skill);

            recognizer.Feed(InputToken.Dodge, 0.0f); // noise
            Assert.Null(recognizer.Feed(InputToken.Jump, 0.2f));
            Assert.Null(recognizer.Feed(InputToken.RightClick, 0.4f));
            Assert.Same(skill, recognizer.Feed(InputToken.LeftClick, 0.6f));
        }

        [Fact]
        public void ShortCombos_AreNotRegistered()
        {
            var recognizer = new ComboRecognizer();
            Assert.False(recognizer.Register(new[] { InputToken.Jump }, Command()));
        }

        [Fact]
        public void Parse_DropsUnknownTokens()
        {
            var combo = InputCombo.Parse(new[] { "Jump", "bogus", "RightClick" });
            Assert.Equal(new[] { InputToken.Jump, InputToken.RightClick }, combo);
        }
    }
}
