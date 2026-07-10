using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class SkillParserTests
    {
        [Fact]
        public void Parse_NullOrEmptyPrimitives_YieldsAnEmptySkill()
        {
            // Graph-only skills (ADR 0007 Phase 4c) carry no primitives — parsing must not throw.
            Assert.Empty(SkillParser.Parse("Graphed", null).Primitives);
            Assert.Empty(SkillParser.Parse("Graphed", System.Array.Empty<string>()).Primitives);
        }

        [Theory]
        [InlineData("Projectile x2 r1 d2", SkillPrimitiveKind.Projectile, 2, 1, 2)]
        [InlineData("DamageOverTime x1 d2", SkillPrimitiveKind.DamageOverTime, 1, 0, 2)]
        [InlineData("Dash x4", SkillPrimitiveKind.Dash, 4, 0, 0)]
        public void ParsesApiPrimitiveFormat(string text, SkillPrimitiveKind kind, int mag, int range, int duration)
        {
            Assert.True(SkillParser.TryParsePrimitive(text, out var p));
            Assert.Equal(kind, p.Kind);
            Assert.Equal(mag, p.Magnitude);
            Assert.Equal(range, p.Range);
            Assert.Equal(duration, p.Duration);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Nonexistent x1")]
        [InlineData("Projectile")]      // no magnitude token
        [InlineData("Projectile x0")]   // magnitude must be positive
        public void RejectsUnusable(string text)
        {
            Assert.False(SkillParser.TryParsePrimitive(text, out _));
        }

        [Fact]
        public void Parse_BuildsSkillSkippingInvalidEntries()
        {
            var skill = SkillParser.Parse("Mix", new[] { "Projectile x2", "garbage", "Area x1" });

            Assert.Equal("Mix", skill.Name);
            Assert.Equal(2, skill.Primitives.Count); // garbage dropped
            Assert.Equal(SkillPrimitiveKind.Projectile, skill.Primitives[0].Kind);
            Assert.Equal(SkillPrimitiveKind.Area, skill.Primitives[1].Kind);
        }
    }
}
