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
}
