using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class TriggerEvaluatorTests
{
    [Fact]
    public void BelowThreshold_DoesNotFire()
    {
        // 30 jumps, no persistence/difficulty/combination → 30 < 100.
        var outcome = TriggerEvaluator.Evaluate(new BehaviorSignature(Frequency: 30, Persistence: 0, Difficulty: 0, Combination: 1));
        Assert.False(outcome.Fires);
        Assert.Equal(30, outcome.Score);
    }

    [Fact]
    public void CrossingThreshold_Fires()
    {
        var outcome = TriggerEvaluator.Evaluate(new BehaviorSignature(Frequency: 100, Persistence: 0, Difficulty: 0, Combination: 1));
        Assert.True(outcome.Fires);
    }

    [Fact]
    public void DifficultyAndCombination_RaiseRarity()
    {
        var common = TriggerEvaluator.Evaluate(new BehaviorSignature(110, 0, 0, 1));
        var rarer = TriggerEvaluator.Evaluate(new BehaviorSignature(110, 4, 5, 3));

        Assert.True(rarer.Score > common.Score);
        Assert.True(rarer.Rarity > common.Rarity); // enum order Common < … < Legendary
    }

    [Fact]
    public void IsDeterministic()
    {
        var a = TriggerEvaluator.Evaluate(new BehaviorSignature(80, 3, 2, 2));
        var b = TriggerEvaluator.Evaluate(new BehaviorSignature(80, 3, 2, 2));
        Assert.Equal(a, b);
    }
}
