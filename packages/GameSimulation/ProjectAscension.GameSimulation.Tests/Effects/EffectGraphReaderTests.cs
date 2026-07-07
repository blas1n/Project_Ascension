using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.GameSimulation.Player;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Effects
{
    /// <summary>
    /// The client parser must read exactly what the server's EffectGraphJson.Serialize writes —
    /// a drift would silently drop movement capabilities. These pin the canonical shapes.
    /// </summary>
    public class EffectGraphReaderTests
    {
        [Fact]
        public void Parse_DoubleJump()
        {
            var node = EffectGraphReader.Parse(
                "{\"trigger\":\"OnJumpInAir\",\"effect\":{\"kind\":\"Impulse\",\"direction\":\"Up\",\"tier\":1}}");
            var trigger = Assert.IsType<Trigger>(node);
            Assert.Equal(TriggerKind.OnJumpInAir, trigger.Kind);
            var impulse = Assert.IsType<Impulse>(trigger.Child);
            Assert.Equal(ImpulseDirection.Up, impulse.Direction);
            Assert.Equal(1, impulse.Tier);
        }

        [Fact]
        public void Parse_WallClimbSequence()
        {
            var node = EffectGraphReader.Parse(
                "{\"trigger\":\"OnWallContact\",\"effect\":{\"kind\":\"Sequence\",\"steps\":[" +
                "{\"kind\":\"Impulse\",\"direction\":\"Up\",\"tier\":2}," +
                "{\"kind\":\"Impulse\",\"direction\":\"Forward\",\"tier\":1}," +
                "{\"kind\":\"Control\",\"effect\":\"Slow\",\"tier\":1}]}}");
            var trigger = Assert.IsType<Trigger>(node);
            Assert.Equal(TriggerKind.OnWallContact, trigger.Kind);
            var seq = Assert.IsType<Sequence>(trigger.Child);
            Assert.Equal(3, seq.Steps.Count);
        }

        [Fact]
        public void Parse_OffensiveSequence()
        {
            var node = EffectGraphReader.Parse(
                "{\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Sequence\",\"steps\":[" +
                "{\"kind\":\"Emit\",\"delivery\":\"Beam\",\"tier\":1},{\"kind\":\"Damage\",\"tier\":2}]}}");
            var trigger = Assert.IsType<Trigger>(node);
            Assert.Equal(TriggerKind.OnCast, trigger.Kind);
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("{\"trigger\":\"OnCast\"}")]                                          // no effect
        [InlineData("{\"trigger\":\"Bogus\",\"effect\":{\"kind\":\"Damage\",\"tier\":1}}")] // bad trigger
        [InlineData("{\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Nope\"}}")]             // unknown node
        [InlineData("")]
        public void Parse_Malformed_ReturnsNull(string json)
            => Assert.Null(EffectGraphReader.Parse(json));

        [Fact]
        public void ParsedGraph_FeedsMovementCapability()
        {
            var graph = EffectGraphReader.Parse(
                "{\"trigger\":\"OnJumpInAir\",\"effect\":{\"kind\":\"Impulse\",\"direction\":\"Up\",\"tier\":1}}");
            var cap = MovementCapability.From(new[] { graph });
            Assert.Equal(1, cap.ExtraJumps);
            Assert.False(cap.WallClimb);
        }
    }
}
