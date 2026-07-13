using ProjectAscension.SkillForge;
using Xunit;

namespace ProjectAscension.Discovery.Tests;

/// <summary>
/// ADR 0008. The engine used to read POSSESSION as synthesis: carry a catalyst and a pistol, shoot a
/// lot, and out came a "flame bullet". These tests pin the correction — a fusion is worth more than a
/// pile of repetition, because a fusion is a decision and repetition is a habit.
/// </summary>
public class SynthesisScoringTests
{
    private static readonly DiscoveryTuning T = DiscoveryTuning.Default;

    private static TriggerOutcome Score(Dictionary<string, int> behaviors, params string[] factors)
        => TriggerEvaluator.Evaluate(
            new BehaviorSignature(behaviors, factors, KnowledgeDepth: 0, Persistence: 0), T);

    [Fact]
    public void AFusion_IsWorthFarMoreThanARepeatedShot()
    {
        // One deliberate act of weaving the catalyst into the gunshot...
        int fused = Score(new() { ["Synthesis:arcane>firearm"] = 1 }).Score;
        // ...against simply pulling the trigger, ten times.
        int sprayed = Score(new() { ["RangedAttack"] = 10 }).Score;

        Assert.True(fused > sprayed,
            $"a fusion ({fused}) must outweigh mere repetition ({sprayed}) — otherwise spam discovers");
    }

    [Fact]
    public void TheOrderOfTheFusion_IsADifferentBehaviour()
    {
        // Same two hands, opposite acts. The engine must see two DIFFERENT behaviours, so they can
        // become two different discoveries (the "same knowledge, different play" promise).
        var wreathed = new Dictionary<string, int> { ["Synthesis:arcane>firearm"] = 2 };
        var detonated = new Dictionary<string, int> { ["Synthesis:firearm>arcane"] = 2 };

        Assert.NotEqual(wreathed.Keys.First(), detonated.Keys.First());
        Assert.Equal(Score(wreathed).Score, Score(detonated).Score); // equally significant...
        // ...but they are not the same key, so the composer is told a different story. That is the point.
    }

    [Fact]
    public void AnyFusionPair_Scores_WithoutBeingSeeded()
    {
        // The prefix rule means a new weapon or element opens new combinations with no new DB rows.
        int known = Score(new() { ["Synthesis:arcane>firearm"] = 1 }).Score;
        int novel = Score(new() { ["Synthesis:venom>bow"] = 1 }).Score;

        Assert.Equal(known, novel);
        Assert.True(novel > T.DefaultBehaviorWeight, "an unseeded fusion must not fall back to the default weight");
    }

    [Fact]
    public void CarryingTwoThings_IsNotFusingThem()
    {
        // The exact bug: the loadout tags are present, the shooting is heavy — and nothing was fused.
        // This may still fire a discovery (it is real play), but it must score BELOW the same play that
        // actually wove the two hands together.
        var carried = Score(new() { ["RangedAttack"] = 12 }, "pistol", "catalyst").Score;
        var fusedToo = Score(new() { ["RangedAttack"] = 12, ["Synthesis:arcane>firearm"] = 3 }, "pistol", "catalyst").Score;

        Assert.True(fusedToo > carried,
            "actually fusing must be worth more than merely holding both while shooting");
    }
}
