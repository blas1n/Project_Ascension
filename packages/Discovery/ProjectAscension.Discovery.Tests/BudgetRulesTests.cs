using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class BudgetRulesTests
{
    private static readonly DiscoveryTuning Tuning = DiscoveryTuning.Default;

    [Fact]
    public void FromScore_ScalesContinuously()
    {
        // Logarithmic (ADR 0010): base 6 + 2.4 × log2(1+score). The budget buys BREADTH of effect, and
        // it deliberately barely moves — doubling the score does not double what you may compose.
        Assert.Equal(22, BudgetRules.FromScore(100, Tuning).Total);
        Assert.Equal(24, BudgetRules.FromScore(200, Tuning).Total);

        // Sixteen times the score buys about a third more expression — and nothing at all in magnitude.
        Assert.True(BudgetRules.FromScore(1600, Tuning).Total < BudgetRules.FromScore(100, Tuning).Total * 2);
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
