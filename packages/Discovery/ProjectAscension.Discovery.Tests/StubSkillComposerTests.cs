using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class StubSkillComposerTests
{
    private static CompositionRequest Req(PrimitiveKind primary, int budget, params string[] tags) =>
        new("theme", tags, primary, new PowerBudget(budget));

    [Fact]
    public void Compose_AlwaysValidatesWithinBudget()
    {
        var budget = new PowerBudget(30);
        var skill = StubSkillComposer.Compose(Req(PrimitiveKind.Projectile, 30, "arcane"));

        var result = CompositionValidator.Validate(skill, budget);
        Assert.True(result.IsValid, $"stub produced an invalid skill: {result.Error}");
    }

    [Fact]
    public void Compose_IsDeterministic()
    {
        var r = Req(PrimitiveKind.Projectile, 30, "arcane", "firearm");
        var a = StubSkillComposer.Compose(r);
        var b = StubSkillComposer.Compose(r);

        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Description, b.Description);
        Assert.True(a.Primitives.SequenceEqual(b.Primitives));
    }

    [Fact]
    public void DifferentContext_YieldsDifferentSkill()
    {
        var arcane = StubSkillComposer.Compose(Req(PrimitiveKind.Projectile, 30, "arcane"));
        var firearm = StubSkillComposer.Compose(Req(PrimitiveKind.Projectile, 30, "firearm"));

        Assert.NotEqual(arcane.Name, firearm.Name); // "Arcane Bolt" vs "Leaden Bolt"
    }

    [Fact]
    public void Compose_IncludesPrimaryBehavior()
    {
        var skill = StubSkillComposer.Compose(Req(PrimitiveKind.Shield, 30, "arcane"));
        Assert.Contains(skill.Primitives, p => p.Kind == PrimitiveKind.Shield);
    }

    [Fact]
    public async Task ComposeAsync_MatchesCompose()
    {
        var r = Req(PrimitiveKind.Dash, 25, "melee");
        var expected = StubSkillComposer.Compose(r);
        var actual = await new StubSkillComposer().ComposeAsync(r);

        Assert.Equal(expected.Name, actual.Name);
        Assert.True(expected.Primitives.SequenceEqual(actual.Primitives));
    }
}
