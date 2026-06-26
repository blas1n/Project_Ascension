using System.Linq;
using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class TriggerEvaluatorTests
{
    private static readonly DiscoveryTuning Tuning = DiscoveryTuning.Default;

    private static BehaviorSignature Sig(int persistence, params (string Behavior, int Count)[] behaviors)
        => new(behaviors.ToDictionary(b => b.Behavior, b => b.Count), persistence);

    [Fact]
    public void BelowThreshold_DoesNotFire()
    {
        // Jump×30 → 30 × weight 1 = 30 < 100.
        var outcome = TriggerEvaluator.Evaluate(Sig(0, ("Jump", 30)), Tuning);
        Assert.False(outcome.Fires);
        Assert.Equal(30, outcome.Score);
    }

    [Fact]
    public void CrossingThreshold_Fires()
    {
        Assert.True(TriggerEvaluator.Evaluate(Sig(0, ("Jump", 100)), Tuning).Fires);
    }

    [Fact]
    public void BehaviorWeight_Matters()
    {
        // Same count, different weight → different score (Jump 1 vs RangedAttack 2).
        Assert.Equal(50, TriggerEvaluator.Evaluate(Sig(0, ("Jump", 50)), Tuning).Score);
        Assert.Equal(100, TriggerEvaluator.Evaluate(Sig(0, ("RangedAttack", 50)), Tuning).Score);
    }

    [Fact]
    public void CombiningBehaviors_AddsSynergy()
    {
        // Jump×50 alone = 50; Jump×50 + MeleeAttack×25 = 50 + 50 + 15 synergy = 115.
        var solo = TriggerEvaluator.Evaluate(Sig(0, ("Jump", 50)), Tuning);
        var combo = TriggerEvaluator.Evaluate(Sig(0, ("Jump", 50), ("MeleeAttack", 25)), Tuning);
        Assert.Equal(50, solo.Score);
        Assert.Equal(115, combo.Score);
    }

    [Fact]
    public void HigherScore_RaisesRarity()
    {
        var low = TriggerEvaluator.Evaluate(Sig(0, ("Jump", 125)), Tuning);          // 125 → Uncommon
        var high = TriggerEvaluator.Evaluate(Sig(0, ("RangedAttack", 130)), Tuning); // 260 → Legendary
        Assert.True(high.Rarity > low.Rarity);
    }

    [Fact]
    public void UnknownBehavior_UsesDefaultWeight()
    {
        // "Sprint" is not in the weights table → default weight 1.
        Assert.Equal(40, TriggerEvaluator.Evaluate(Sig(0, ("Sprint", 40)), Tuning).Score);
    }

    [Fact]
    public void Persistence_AddsToScore()
    {
        // Jump×10 + persistence 4 × 5 = 10 + 20 = 30.
        Assert.Equal(30, TriggerEvaluator.Evaluate(Sig(4, ("Jump", 10)), Tuning).Score);
    }

    [Fact]
    public void IsDeterministic()
    {
        var a = TriggerEvaluator.Evaluate(Sig(3, ("Jump", 40), ("Dodge", 20)), Tuning);
        var b = TriggerEvaluator.Evaluate(Sig(3, ("Jump", 40), ("Dodge", 20)), Tuning);
        Assert.Equal(a, b);
    }
}
