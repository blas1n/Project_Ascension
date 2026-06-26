using ProjectAscension.SkillForge;

namespace ProjectAscension.SkillForge.Tests;

public class CompositionPipelineTests
{
    private static CompositionRequest Req(int budget = 30) =>
        new("theme", new[] { "arcane" }, PrimitiveKind.Projectile, new PowerBudget(budget));

    /// <summary>Always returns an over-budget skill (Area×5 = 60) → invalid.</summary>
    private sealed class OverBudgetComposer : ISkillComposer
    {
        public int Calls { get; private set; }

        public Task<SkillComposition> ComposeAsync(CompositionRequest request, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new SkillComposition(
                "Too Big", "desc", new[] { new ComposedPrimitive(PrimitiveKind.Area, 5) }));
        }
    }

    /// <summary>Invalid for the first <c>failFor</c> calls, then a valid cheap skill.</summary>
    private sealed class FlakyComposer : ISkillComposer
    {
        private readonly int _failFor;
        public int Calls { get; private set; }

        public FlakyComposer(int failFor) => _failFor = failFor;

        public Task<SkillComposition> ComposeAsync(CompositionRequest request, CancellationToken ct = default)
        {
            Calls++;
            var primitives = Calls <= _failFor
                ? new[] { new ComposedPrimitive(PrimitiveKind.Area, 5) }       // 60 > budget
                : new[] { new ComposedPrimitive(PrimitiveKind.Knockback, 1) }; // 5, valid
            return Task.FromResult(new SkillComposition("Skill", "desc", primitives));
        }
    }

    [Fact]
    public async Task Stub_ForgesOnFirstAttempt()
    {
        var outcome = await CompositionPipeline.ForgeAsync(Req(), new StubSkillComposer());

        Assert.True(outcome.Forged);
        Assert.NotNull(outcome.Skill);
        Assert.Equal(1, outcome.Attempts);
    }

    [Fact]
    public async Task InvalidOutput_RetriesUpToMax_ThenDefers()
    {
        var composer = new OverBudgetComposer();
        var outcome = await CompositionPipeline.ForgeAsync(Req(30), composer, maxAttempts: 3);

        Assert.False(outcome.Forged);
        Assert.Null(outcome.Skill);
        Assert.Equal(3, outcome.Attempts);
        Assert.Equal(3, composer.Calls);
        Assert.Equal(CompositionError.OverBudget, outcome.LastValidation.Error);
    }

    [Fact]
    public async Task RetriesThenSucceeds()
    {
        var composer = new FlakyComposer(failFor: 2);
        var outcome = await CompositionPipeline.ForgeAsync(Req(30), composer, maxAttempts: 5);

        Assert.True(outcome.Forged);
        Assert.Equal(3, outcome.Attempts); // failed twice, succeeded on the 3rd
        Assert.Equal(PrimitiveKind.Knockback, outcome.Skill!.Primitives[0].Kind);
    }
}
