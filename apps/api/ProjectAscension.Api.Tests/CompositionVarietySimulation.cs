using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OllamaSharp;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.SkillForge;
using Xunit;
using Xunit.Abstractions;

namespace ProjectAscension.Api.Tests;

/// <summary>
/// Simulation test for composition variety — the thing we kept checking by hand in Unity.
/// It drives a broad set of synthetic "play sessions" (varied attacks, mobility, weapons)
/// straight through the real LLM composer and observes whether the composed skills stay
/// varied AND whether the delivery generalizes to the attack×mobility grid (a guard against
/// overfitting the prompt to a handful of cases). No Unity, no server, no DB.
///
/// Gated on OLLAMA_ENDPOINT so CI skips it. Run on demand:
///   OLLAMA_ENDPOINT=http://host:11434 OLLAMA_MODEL=qwen3-coder:30b \
///     dotnet test --filter FullyQualifiedName~CompositionVarietySimulation
/// (or tools/discovery-variety-sim.sh).
/// </summary>
public class CompositionVarietySimulation
{
    private readonly ITestOutputHelper _out;
    public CompositionVarietySimulation(ITestOutputHelper output) => _out = output;

    private sealed record Scenario(string Name, PrimitiveKind Primary, string[] Tags, (string Behavior, int Count)[] Play);

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

        // A broad, diverse set — the grid cells, boundary mobility, mixed/ambiguous attacks,
        // and three weapon primaries / four equipment tags — to observe generalization.
        var scenarios = new[]
        {
            new Scenario("charge-still", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("ChargedAttack", 60) }),
            new Scenario("charge-mobile", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("ChargedAttack", 40), ("Jump", 35) }),
            new Scenario("rapid-still", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("RangedAttack", 60) }),
            new Scenario("rapid-mobile", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("RangedAttack", 40), ("Dodge", 35) }),
            new Scenario("melee-still", PrimitiveKind.Area, new[] { "blade" }, new[] { ("MeleeAttack", 60) }),
            new Scenario("melee-mobile", PrimitiveKind.Area, new[] { "blade" }, new[] { ("MeleeAttack", 50), ("Jump", 30) }),
            new Scenario("charge-lightjump", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("ChargedAttack", 60), ("Jump", 12) }),
            new Scenario("rapid-heavymove", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("RangedAttack", 40), ("Dodge", 30), ("Jump", 20) }),
            new Scenario("mixed-charge-dom", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("ChargedAttack", 40), ("RangedAttack", 25) }),
            new Scenario("bow-rapid", PrimitiveKind.Projectile, new[] { "bow" }, new[] { ("RangedAttack", 60) }),
            new Scenario("bow-mobile", PrimitiveKind.Projectile, new[] { "bow" }, new[] { ("RangedAttack", 35), ("Dodge", 40) }),
            new Scenario("pistol-charge", PrimitiveKind.Beam, new[] { "pistol" }, new[] { ("ChargedAttack", 50), ("Dodge", 10) }),
        };

        var deliveries = new List<string>();
        var effectSignatures = new List<string>();
        int matches = 0;

        _out.WriteLine($"model: {model}");
        _out.WriteLine($"{"play",-17} | {"expect",-11} | {"actual",-11} | {"ok",-4} | primitives");
        for (int i = 0; i < scenarios.Length; i++)
        {
            var s = scenarios[i];
            var counts = s.Play.Select(p => new BehaviorCount(p.Behavior, p.Count)).ToList();
            var profile = counts.Select(c => new BehaviorWeight(c.Behavior, c.Count)).ToList();
            var request = new CompositionRequest(
                "an expedition discovery", s.Tags, s.Primary, new PowerBudget(50),
                Lineage: null, BehaviorProfile: profile, Seed: 1000 + i);

            var outcome = await CompositionPipeline.ForgeAsync(request, composer, maxAttempts: 3);
            Assert.True(outcome.Forged && outcome.Skill is not null,
                $"{s.Name}: composition deferred ({outcome.LastValidation.Error}).");

            var skill = outcome.Skill!;
            var expected = DeliveryHeuristics.ForBehavior(counts); // the grid the prompt guides toward
            bool ok = string.Equals(skill.Delivery, expected, StringComparison.OrdinalIgnoreCase);
            if (ok) matches++;
            var prims = string.Join(",", skill.Primitives.Select(p => $"{p.Kind}x{p.Magnitude}"));
            deliveries.Add(skill.Delivery);
            effectSignatures.Add(prims);
            _out.WriteLine($"{s.Name,-17} | {expected,-11} | {skill.Delivery,-11} | {(ok ? "ok" : "MISS"),-4} | {prims}");
        }

        int distinctEffects = effectSignatures.Distinct().Count();
        int distinctDeliveries = deliveries.Distinct().Count();
        double matchRate = (double)matches / scenarios.Length;
        _out.WriteLine($"\ndistinct EFFECTS: {distinctEffects}/{scenarios.Length}" +
                       $" | distinct DELIVERIES: {distinctDeliveries}/5 styles" +
                       $" | delivery matches the play grid: {matches}/{scenarios.Length} ({matchRate:P0})");

        // Effects must stay varied (the "static recipe" regression guard).
        Assert.True(distinctEffects >= scenarios.Length - 3,
            $"effect variety too low ({distinctEffects}/{scenarios.Length}) — different play produced near-identical primitives.");
        // The prompt-guided delivery should generalize to unseen scenarios, not be overfit to a
        // few — most should land on the play grid.
        Assert.True(matchRate >= 0.75,
            $"delivery generalization too low ({matchRate:P0}) — the prompt may be overfit; observe the MISS rows and tune.");
    }
}
