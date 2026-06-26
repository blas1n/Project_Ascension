using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class BudgetRulesTests
{
    [Theory]
    [InlineData(Rarity.Common, 20)]
    [InlineData(Rarity.Rare, 32)]
    [InlineData(Rarity.Legendary, 50)]
    public void DeriveMapsRarityToBudget(Rarity rarity, int expected)
        => Assert.Equal(expected, BudgetRules.Derive(rarity).Total);

    [Fact]
    public void RarityIsMonotonic()
    {
        Assert.True(BudgetRules.Derive(Rarity.Common).Total
            < BudgetRules.Derive(Rarity.Rare).Total);
        Assert.True(BudgetRules.Derive(Rarity.Rare).Total
            < BudgetRules.Derive(Rarity.Legendary).Total);
    }
}
