using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class BudgetRulesTests
{
    private static readonly DiscoveryTuning Tuning = DiscoveryTuning.Default;

    [Fact]
    public void FromScore_ScalesContinuously()
    {
        // base 8 + score × 0.18: 100 → 26, 200 → 44.
        Assert.Equal(26, BudgetRules.FromScore(100, Tuning).Total);
        Assert.Equal(44, BudgetRules.FromScore(200, Tuning).Total);
    }

    [Fact]
    public void FromScore_IsMonotonic()
    {
        Assert.True(BudgetRules.FromScore(120, Tuning).Total < BudgetRules.FromScore(220, Tuning).Total);
    }

    [Fact]
    public void FromScore_ClampsToRange()
    {
        Assert.Equal(Tuning.BudgetMin, BudgetRules.FromScore(0, Tuning).Total);
        Assert.Equal(Tuning.BudgetMax, BudgetRules.FromScore(100_000, Tuning).Total);
    }

    [Fact]
    public void FromRarity_IsMonotonic()
    {
        Assert.True(BudgetRules.FromRarity(Rarity.Common, Tuning).Total
            < BudgetRules.FromRarity(Rarity.Rare, Tuning).Total);
        Assert.True(BudgetRules.FromRarity(Rarity.Rare, Tuning).Total
            < BudgetRules.FromRarity(Rarity.Legendary, Tuning).Total);
    }
}
