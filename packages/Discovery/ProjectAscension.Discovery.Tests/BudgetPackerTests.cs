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
    public void PreservesBreadth_KeepsSeveralPrimitivesNotJustTheFirst()
    {
        // The AI proposes 4 high-magnitude primitives; the packer keeps several at
        // low magnitude rather than collapsing the whole budget into the first.
        var packed = BudgetPacker.Pack(
            new[]
            {
                new ComposedPrimitive(PrimitiveKind.Projectile, 4),
                new ComposedPrimitive(PrimitiveKind.Homing, 3),
                new ComposedPrimitive(PrimitiveKind.Pierce, 4),
                new ComposedPrimitive(PrimitiveKind.DamageOverTime, 2),
            },
            new PowerBudget(30));

        Assert.True(packed.Count >= 3, $"expected breadth, got {packed.Count}");
        Assert.Equal(PrimitiveKind.Projectile, packed[0].Kind); // priority order preserved
        Assert.True(Cost(packed) <= 30);
    }

    [Fact]
    public void SkipsPrimitivesThatDoNotFitAtMagnitudeOne()
    {
        // Area(12) + Projectile(10) = 22; Shield(10) would be 32 > 25, so it's dropped.
        var packed = BudgetPacker.Pack(
            new[]
            {
                new ComposedPrimitive(PrimitiveKind.Area, 1),
                new ComposedPrimitive(PrimitiveKind.Projectile, 1),
                new ComposedPrimitive(PrimitiveKind.Shield, 1),
            },
            new PowerBudget(25));

        Assert.Equal(2, packed.Count);
        Assert.DoesNotContain(packed, p => p.Kind == PrimitiveKind.Shield);
        Assert.True(Cost(packed) <= 25);
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

    [Fact]
    public void Parameters_ArePackedWithinBudget()
    {
        var packed = BudgetPacker.Pack(
            new[] { new ComposedPrimitive(PrimitiveKind.Projectile, 2, Range: 2, Duration: 2) },
            new PowerBudget(30));

        var p = Assert.Single(packed);
        Assert.True(CompositionValidator.CostOf(p) <= 30);
        // Leftover budget was spent raising potency and/or parameters past the base.
        Assert.True(p.Magnitude > 1 || p.Range > 0 || p.Duration > 0);
    }
}
