using ProjectAscension.Discovery;

namespace ProjectAscension.Discovery.Tests;

public class CompositionValidatorTests
{
    private static SkillComposition Skill(params ComposedPrimitive[] primitives) =>
        new("Test Skill", "desc", primitives);

    [Fact]
    public void Valid_WithinBudget_ReturnsOkAndTotalCost()
    {
        // Projectile(10) + DamageOverTime(8) = 18 <= 30
        var skill = Skill(new ComposedPrimitive(PrimitiveKind.Projectile, 1), new ComposedPrimitive(PrimitiveKind.DamageOverTime, 1));
        var result = CompositionValidator.Validate(skill, new PowerBudget(30));

        Assert.True(result.IsValid);
        Assert.Equal(CompositionError.None, result.Error);
        Assert.Equal(18, result.TotalCost);
    }

    [Fact]
    public void Magnitude_ScalesCost()
    {
        // Projectile(10) * 2 = 20
        var skill = Skill(new ComposedPrimitive(PrimitiveKind.Projectile, 2));
        Assert.Equal(20, CompositionValidator.Validate(skill, new PowerBudget(20)).TotalCost);
    }

    [Fact]
    public void OverBudget_Fails_WithComputedCost()
    {
        // Projectile(10) + Area(12) = 22 > 20
        var skill = Skill(new ComposedPrimitive(PrimitiveKind.Projectile, 1), new ComposedPrimitive(PrimitiveKind.Area, 1));
        var result = CompositionValidator.Validate(skill, new PowerBudget(20));

        Assert.False(result.IsValid);
        Assert.Equal(CompositionError.OverBudget, result.Error);
        Assert.Equal(22, result.TotalCost);
    }

    [Fact]
    public void Empty_Fails()
    {
        Assert.Equal(CompositionError.EmptyComposition,
            CompositionValidator.Validate(Skill(), new PowerBudget(30)).Error);
        Assert.Equal(CompositionError.EmptyComposition,
            CompositionValidator.Validate(null, new PowerBudget(30)).Error);
    }

    [Fact]
    public void MissingName_Fails()
    {
        var skill = new SkillComposition("  ", "desc", new ComposedPrimitive[] { new ComposedPrimitive(PrimitiveKind.Dash, 1) });
        Assert.Equal(CompositionError.MissingName, CompositionValidator.Validate(skill, new PowerBudget(30)).Error);
    }

    [Fact]
    public void UnknownPrimitive_Fails()
    {
        var skill = Skill(new ComposedPrimitive((PrimitiveKind)999, 1));
        Assert.Equal(CompositionError.UnknownPrimitive, CompositionValidator.Validate(skill, new PowerBudget(30)).Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(CompositionValidator.MaxMagnitude + 1)]
    public void InvalidMagnitude_Fails(int magnitude)
    {
        var skill = Skill(new ComposedPrimitive(PrimitiveKind.Dash, magnitude));
        Assert.Equal(CompositionError.InvalidMagnitude, CompositionValidator.Validate(skill, new PowerBudget(100)).Error);
    }
}
