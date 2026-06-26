using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class BudgetPackerTests
{
    private static int Cost(IReadOnlyList<ComposedPrimitive> ps) =>
        ps.Sum(p => PrimitiveCatalog.BaseCostOf(p.Kind) * p.Magnitude);

    [Fact]
    public void KeepsProposalThatAlreadyFits()
    {
        var packed = BudgetPacker.Pack(
            new[] { new ComposedPrimitive(PrimitiveKind.Projectile, 1), new ComposedPrimitive(PrimitiveKind.Knockback, 1) },
            new PowerBudget(30));

        Assert.Equal(2, packed.Count);
        Assert.Equal(15, Cost(packed));
    }

    [Fact]
    public void ClampsMagnitudeToBudget()
    {
        var packed = BudgetPacker.Pack(new[] { new ComposedPrimitive(PrimitiveKind.Projectile, 5) }, new PowerBudget(30));

        Assert.Single(packed);
        Assert.Equal(3, packed[0].Magnitude); // 10 * 3 = 30
    }

    [Fact]
    public void DropsPrimitivesThatDoNotFitAfterSpend()
    {
        var packed = BudgetPacker.Pack(
            new[] { new ComposedPrimitive(PrimitiveKind.Projectile, 5), new ComposedPrimitive(PrimitiveKind.Homing, 1) },
            new PowerBudget(30));

        Assert.Single(packed); // Projectile clamps to mag 3 (=30), leaving nothing for Homing
        Assert.Equal(PrimitiveKind.Projectile, packed[0].Kind);
        Assert.True(Cost(packed) <= 30);
    }

    [Fact]
    public void SkipsUnknownKinds()
    {
        var packed = BudgetPacker.Pack(
            new[] { new ComposedPrimitive((PrimitiveKind)999, 1), new ComposedPrimitive(PrimitiveKind.Dash, 1) },
            new PowerBudget(30));

        Assert.Single(packed);
        Assert.Equal(PrimitiveKind.Dash, packed[0].Kind);
    }

    [Fact]
    public void EmptyWhenBudgetBelowCheapest()
    {
        var packed = BudgetPacker.Pack(new[] { new ComposedPrimitive(PrimitiveKind.Knockback, 1) }, new PowerBudget(3));
        Assert.Empty(packed); // cheapest base cost is 5 > 3
    }

    [Fact]
    public void ResultAlwaysWithinBudget()
    {
        var packed = BudgetPacker.Pack(
            new[]
            {
                new ComposedPrimitive(PrimitiveKind.Area, 5),
                new ComposedPrimitive(PrimitiveKind.DamageOverTime, 5),
                new ComposedPrimitive(PrimitiveKind.Projectile, 5),
            },
            new PowerBudget(40));

        Assert.NotEmpty(packed);
        Assert.True(CompositionValidator.Validate(new SkillComposition("x", "d", packed), new PowerBudget(40)).IsValid);
    }
}
