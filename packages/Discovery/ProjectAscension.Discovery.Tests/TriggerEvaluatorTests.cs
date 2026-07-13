using System;
using System.Linq;
using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class TriggerEvaluatorTests
{
    private static readonly DiscoveryTuning Tuning = DiscoveryTuning.Default;

    private static BehaviorSignature Sig(int persistence, params (string Behavior, int Count)[] behaviors)
        => new(behaviors.ToDictionary(b => b.Behavior, b => b.Count), Array.Empty<string>(), 0, persistence);

    private static BehaviorSignature SigF(string[] factors, params (string Behavior, int Count)[] behaviors)
        => new(behaviors.ToDictionary(b => b.Behavior, b => b.Count), factors, 0, 0);

    private static BehaviorSignature SigD(int knowledgeDepth, params (string Behavior, int Count)[] behaviors)
        => new(behaviors.ToDictionary(b => b.Behavior, b => b.Count), Array.Empty<string>(), knowledgeDepth, 0);

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
    public void ContextFactors_AddSignificanceAndSynergy()
    {
        // Same behavior, no factors vs at a waterfall (weight 10):
        // bare = 50; with factor = 50 + 10 + (2 distinct − 1) × 15 synergy = 75.
        var bare = TriggerEvaluator.Evaluate(Sig(0, ("Jump", 50)), Tuning);
        var withFactor = TriggerEvaluator.Evaluate(SigF(new[] { "waterfall" }, ("Jump", 50)), Tuning);
        Assert.Equal(50, bare.Score);
        Assert.Equal(75, withFactor.Score);
    }

    [Fact]
    public void SameBehavior_DifferentEnvironment_ScoresDifferently()
    {
        // jungle (8) vs crystal_desert (12) — the same behavior discovers differently.
        var jungle = TriggerEvaluator.Evaluate(SigF(new[] { "jungle" }, ("Jump", 50)), Tuning);
        var desert = TriggerEvaluator.Evaluate(SigF(new[] { "crystal_desert" }, ("Jump", 50)), Tuning);
        Assert.NotEqual(jungle.Score, desert.Score);
    }

    [Fact]
    public void UnknownFactor_DoesNotAffectScore()
    {
        // "arcane" is not a weighted factor → no score change, no synergy.
        Assert.Equal(50, TriggerEvaluator.Evaluate(SigF(new[] { "arcane" }, ("Jump", 50)), Tuning).Score);
    }

    [Fact]
    public void KnowledgeDepth_DeepensDiscovery()
    {
        // DEPTH NO LONGER ADDS SCORE (ADR 0010). It used to — and that was an inflation vector: the
        // same play scored higher every time simply because you had discovered here before, so a
        // player could climb the rarity ladder without ever doing anything new. "발견은 다음 발견의
        // 시작" is honoured through LINEAGE (the composer evolves the ancestors), which enriches what
        // the discovery IS — not by making the next one cheaper to get.
        var shallow = TriggerEvaluator.Evaluate(SigD(0, ("Jump", 50)), Tuning);
        var deep = TriggerEvaluator.Evaluate(SigD(2, ("Jump", 50)), Tuning);
        Assert.Equal(50, shallow.Score);
        Assert.Equal(shallow.Score, deep.Score); // the same play is worth the same, however deep you are
    }

    [Fact]
    public void IsDeterministic()
    {
        var a = TriggerEvaluator.Evaluate(Sig(3, ("Jump", 40), ("MeleeAttack", 20)), Tuning);
        var b = TriggerEvaluator.Evaluate(Sig(3, ("Jump", 40), ("MeleeAttack", 20)), Tuning);
        Assert.Equal(a, b);
    }
}
