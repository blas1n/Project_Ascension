using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OllamaSharp;
using ProjectAscension.Api.Services;
using ProjectAscension.SkillForge;
using Xunit;
using Xunit.Abstractions;

namespace ProjectAscension.Api.Tests;

/// <summary>
/// Simulation test for composition variety — the thing we kept checking by hand in Unity.
/// It drives synthetic "play sessions" (behavior profiles for distinct play styles) straight
/// through the real LLM composer and asserts the resulting skills are actually varied (the
/// "static recipe" regression this guards against). No Unity, no server, no DB.
///
/// It needs a reachable Ollama, so it is gated on the OLLAMA_ENDPOINT env var and simply
/// skips (returns) when unset — CI does not set it. Run it on demand:
///   OLLAMA_ENDPOINT=http://host:11434 OLLAMA_MODEL=qwen3-coder:30b \
///     dotnet test --filter FullyQualifiedName~CompositionVarietySimulation
/// (or via tools/discovery-variety-sim.sh, which wraps the Docker invocation.)
/// </summary>
public class CompositionVarietySimulation
{
    private readonly ITestOutputHelper _out;
    public CompositionVarietySimulation(ITestOutputHelper output) => _out = output;

    private sealed record Scenario(string Name, PrimitiveKind Primary, (string Behavior, int Count)[] Play);

    [Fact]
    public async Task VariedPlay_ProducesVariedSkills()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _out.WriteLine("SKIPPED: set OLLAMA_ENDPOINT (+ optional OLLAMA_MODEL) to run the live variety simulation.");
            return;
        }
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3-coder:30b";

        IChatClient chat = new OllamaApiClient(new Uri(endpoint), model);
        var composer = new LlmSkillComposer(
            chat, new LlmComposerOptions { Timeout = TimeSpan.FromSeconds(90) }, NullLogger<LlmSkillComposer>.Instance);

        // Distinct play styles that MUST yield distinct skills (same equipment budget/theme).
        var scenarios = new[]
        {
            new Scenario("charge-heavy", PrimitiveKind.Beam, new[] { ("ChargedAttack", 60) }),
            new Scenario("rapid-ranged", PrimitiveKind.Beam, new[] { ("RangedAttack", 60) }),
            new Scenario("mobile-ranged", PrimitiveKind.Beam, new[] { ("RangedAttack", 40), ("Dodge", 30), ("Jump", 25) }),
            new Scenario("melee-combo", PrimitiveKind.Area, new[] { ("MeleeAttack", 60) }),
            new Scenario("charge-mobile", PrimitiveKind.Beam, new[] { ("ChargedAttack", 40), ("Jump", 30) }),
        };

        var rawLlmDeliveries = new List<string>();
        var effectSignatures = new List<string>();

        _out.WriteLine($"model: {model}");
        _out.WriteLine($"{"play",-14} | {"llmDelivery",-11} | {"name",-30} | primitives");
        for (int i = 0; i < scenarios.Length; i++)
        {
            var s = scenarios[i];
            var profile = s.Play.Select(p => new BehaviorWeight(p.Behavior, p.Count)).ToList();
            var request = new CompositionRequest(
                "an expedition discovery", new[] { "arcane" }, s.Primary, new PowerBudget(50),
                Lineage: null, BehaviorProfile: profile, Seed: 1000 + i);

            var outcome = await CompositionPipeline.ForgeAsync(request, composer, maxAttempts: 3);
            Assert.True(outcome.Forged && outcome.Skill is not null,
                $"{s.Name}: composition deferred ({outcome.LastValidation.Error}).");

            var skill = outcome.Skill!;
            var prims = string.Join(",", skill.Primitives.Select(p => $"{p.Kind}x{p.Magnitude}"));
            rawLlmDeliveries.Add(skill.Delivery);
            effectSignatures.Add(prims);
            _out.WriteLine($"{s.Name,-14} | {skill.Delivery,-11} | {skill.Name,-30} | {prims}");
        }

        int distinctEffects = effectSignatures.Distinct().Count();
        _out.WriteLine($"\ndistinct EFFECTS (LLM's job): {distinctEffects}/{scenarios.Length}" +
                       $" | distinct LLM deliveries (informational — production derives delivery" +
                       $" from play, not the LLM): {rawLlmDeliveries.Distinct().Count()}/{scenarios.Length}");

        // The LLM owns the EFFECT (primitives); this guards its variety against the
        // "static recipe" regression. Delivery is derived deterministically from play (the
        // LLM's own delivery pick converges), so it is reported but not asserted here — the
        // DeliveryForBehavior mapping is covered by fast deterministic tests instead.
        Assert.True(distinctEffects >= 4,
            $"effect variety too low ({distinctEffects}/{scenarios.Length}) — different play produced near-identical primitives.");
    }
}
