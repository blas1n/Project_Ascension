using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class PrimitiveCatalogTests
{
    [Fact]
    public void All_AreDistinctKinds()
    {
        var kinds = PrimitiveCatalog.All.Select(d => d.Kind).ToList();
        Assert.Equal(kinds.Count, kinds.Distinct().Count());
        Assert.NotEmpty(kinds);
    }

    [Fact]
    public void IsKnown_TrueForCatalog_FalseForOutOfRange()
    {
        Assert.True(PrimitiveCatalog.IsKnown(PrimitiveKind.Projectile));
        Assert.False(PrimitiveCatalog.IsKnown((PrimitiveKind)999));
    }

    [Fact]
    public void BaseCostOf_ReturnsDefinedCost()
    {
        Assert.Equal(10, PrimitiveCatalog.BaseCostOf(PrimitiveKind.Projectile));
        Assert.Equal(5, PrimitiveCatalog.BaseCostOf(PrimitiveKind.Knockback));
    }

    [Fact]
    public void BaseCostOf_ThrowsOnUnknown()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PrimitiveCatalog.BaseCostOf((PrimitiveKind)999));
    }

    [Fact]
    public void Catalog_CoversAllCategories()
    {
        var categories = PrimitiveCatalog.All.Select(d => d.Category).Distinct().ToList();
        Assert.Contains(PrimitiveCategory.Offensive, categories);
        Assert.Contains(PrimitiveCategory.Control, categories);
        Assert.Contains(PrimitiveCategory.Mobility, categories);
        Assert.Contains(PrimitiveCategory.Defensive, categories);
    }

    [Fact]
    public void EveryDefinedKindIsInTheCatalog()
    {
        // Guards against adding a PrimitiveKind enum value without a catalog entry.
        foreach (PrimitiveKind kind in Enum.GetValues<PrimitiveKind>())
            Assert.True(PrimitiveCatalog.IsKnown(kind), $"{kind} missing from catalog");
    }

    [Theory]
    [InlineData(PrimitiveKind.Chain)]
    [InlineData(PrimitiveKind.Beam)]
    [InlineData(PrimitiveKind.Stun)]
    [InlineData(PrimitiveKind.Blink)]
    [InlineData(PrimitiveKind.Barrier)]
    [InlineData(PrimitiveKind.Leech)]
    public void ExpandedPrimitivesAreKnown(PrimitiveKind kind) => Assert.True(PrimitiveCatalog.IsKnown(kind));
}
