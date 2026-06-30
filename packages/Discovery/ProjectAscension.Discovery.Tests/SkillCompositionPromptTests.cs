using ProjectAscension.SkillForge;
using Xunit;

namespace ProjectAscension.Discovery.Tests;

// The composer's seed must carry HOW the player fought, or the same combination collapses
// to one identical skill (the "static recipe" failure mode). These lock that the behavior
// profile reaches the prompt and that different profiles produce different prompts.
public class SkillCompositionPromptTests
{
    private static CompositionRequest Req(params BehaviorWeight[] profile) =>
        new("an expedition discovery", new[] { "arcane" }, PrimitiveKind.Beam, new PowerBudget(40),
            Lineage: null, BehaviorProfile: profile);

    [Fact]
    public void Build_IncludesTheBehaviorProfile()
    {
        var prompt = SkillCompositionPrompt.Build(
            Req(new BehaviorWeight("RangedAttack", 200), new BehaviorWeight("ChargedAttack", 30)));

        Assert.Contains("How the player fought", prompt);
        Assert.Contains("RangedAttack: 200", prompt);
        Assert.Contains("ChargedAttack: 30", prompt);
    }

    [Fact]
    public void Build_DifferentBehavior_ProducesDifferentPrompts()
    {
        // Same combination (theme/context/primary/budget), different play → different prompt,
        // so the composer can shape a different skill. This is the "behavior must matter" hook.
        var sustainedCharger = SkillCompositionPrompt.Build(Req(new BehaviorWeight("ChargedAttack", 120)));
        var mobileSkirmisher = SkillCompositionPrompt.Build(
            Req(new BehaviorWeight("RangedAttack", 90), new BehaviorWeight("Dodge", 80)));

        Assert.NotEqual(sustainedCharger, mobileSkirmisher);
    }

    [Fact]
    public void Build_NoProfile_OmitsTheSection()
    {
        var prompt = SkillCompositionPrompt.Build(
            new CompositionRequest("t", new[] { "arcane" }, PrimitiveKind.Beam, new PowerBudget(40)));

        Assert.DoesNotContain("How the player fought", prompt);
    }
}
