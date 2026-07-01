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
            // Edge cases: ambiguous mix, exact tie, and extreme intensities.
            new Scenario("triple-mixed", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("ChargedAttack", 30), ("RangedAttack", 28), ("MeleeAttack", 25) }),
            new Scenario("tie-mobility", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("RangedAttack", 40), ("Dodge", 40) }),
            new Scenario("mixed-rapid-dom", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("RangedAttack", 45), ("ChargedAttack", 30) }),
            new Scenario("extreme-charge", PrimitiveKind.Beam, new[] { "arcane" }, new[] { ("ChargedAttack", 300) }),
            new Scenario("extreme-melee", PrimitiveKind.Area, new[] { "blade" }, new[] { ("MeleeAttack", 200) }),
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

    /// <summary>
    /// The discovery graph: a skill composed from prior discoveries (lineage RAG), which is
    /// effectively infinite in this game — each discovery is the seed of the next. This drives
    /// a chain where every generation is composed with the previous ones as lineage and checks
    /// it keeps EVOLVING (the prompt's "extend, do not merely repeat"), not collapsing into a
    /// repeated skill.
    /// </summary>
    [Fact]
    public async Task LineageChain_KeepsEvolving_DoesNotRepeat()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _out.WriteLine("SKIPPED: set OLLAMA_ENDPOINT to run the lineage-chain simulation.");
            return;
        }
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3-coder:30b";
        IChatClient chat = new OllamaApiClient(new Uri(endpoint), model);
        var composer = new LlmSkillComposer(
            chat, new LlmComposerOptions { Timeout = TimeSpan.FromSeconds(90) }, NullLogger<LlmSkillComposer>.Instance);

        // The realistic graph: each generation is a DIFFERENT play (a new discovery) built on
        // the growing lineage of prior discoveries. The question is whether the lineage still
        // lets the current play drive the skill, or drags every generation toward the ancestors.
        (string Name, PrimitiveKind Primary, (string, int)[] Play)[] gens =
        {
            ("charge-still", PrimitiveKind.Beam, new[] { ("ChargedAttack", 60) }),
            ("rapid-still", PrimitiveKind.Beam, new[] { ("RangedAttack", 60) }),
            ("melee", PrimitiveKind.Area, new[] { ("MeleeAttack", 60) }),
            ("charge-mobile", PrimitiveKind.Beam, new[] { ("ChargedAttack", 40), ("Jump", 35) }),
            ("rapid-mobile", PrimitiveKind.Beam, new[] { ("RangedAttack", 40), ("Dodge", 35) }),
            ("charge-still-2", PrimitiveKind.Beam, new[] { ("ChargedAttack", 70) }),
            ("melee-mobile", PrimitiveKind.Area, new[] { ("MeleeAttack", 50), ("Jump", 30) }),
            ("rapid-still-2", PrimitiveKind.Projectile, new[] { ("RangedAttack", 55) }),
        };
        var lineage = new List<PriorArt>();
        var effectSignatures = new List<string>();
        var deliveries = new List<string>();

        _out.WriteLine($"model: {model} | different play each generation, lineage accumulates");
        _out.WriteLine($"{"gen",-14} | {"delivery",-11} | {"name",-28} | primitives");
        for (int gen = 0; gen < gens.Length; gen++)
        {
            var g = gens[gen];
            var profile = g.Play.Select(p => new BehaviorWeight(p.Item1, p.Item2)).ToList();
            var request = new CompositionRequest(
                "an expedition discovery", new[] { "arcane" }, g.Primary, new PowerBudget(50),
                // Feed the most recent ancestors, like the service's RAG (bounded).
                Lineage: lineage.TakeLast(4).ToList(), BehaviorProfile: profile, Seed: 2000 + gen);

            var outcome = await CompositionPipeline.ForgeAsync(request, composer, maxAttempts: 3);
            Assert.True(outcome.Forged && outcome.Skill is not null,
                $"{g.Name}: composition deferred ({outcome.LastValidation.Error}).");

            var skill = outcome.Skill!;
            var prims = string.Join(",", skill.Primitives.Select(p => $"{p.Kind}x{p.Magnitude}"));
            lineage.Add(new PriorArt(skill.Name, skill.Description ?? string.Empty, skill.Primitives));
            effectSignatures.Add(prims);
            deliveries.Add(skill.Delivery);
            _out.WriteLine($"{g.Name,-14} | {skill.Delivery,-11} | {skill.Name,-28} | {prims}");
        }

        int distinct = effectSignatures.Distinct().Count();
        _out.WriteLine($"\ndistinct skills across the chain: {distinct}/{gens.Length}" +
                       $" | distinct deliveries: {deliveries.Distinct().Count()}/5");

        // Even with a growing lineage, different play must still yield different skills — the
        // graph must not drag every generation toward its ancestors.
        Assert.True(distinct >= 4,
            $"lineage chain converged ({distinct}/{gens.Length} distinct) — the lineage dragged generations toward the ancestors instead of letting the current play drive; strengthen the prompt.");
    }

    /// <summary>
    /// The real duplicate scenario the earlier sim missed: the SAME play fires several
    /// discoveries in a burst, each composed with the prior ones as lineage (the RAG). The
    /// retry-on-duplicate (CompositionPipeline) must keep them mechanically distinct — no six
    /// "Ethereal Weaving Volley"s. The old sim fed perfect lineage AND I dismissed its
    /// converging same-play result; this asserts distinctness instead.
    /// </summary>
    [Fact]
    public async Task SamePlayBurst_RetryKeepsSkillsDistinct()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _out.WriteLine("SKIPPED: set OLLAMA_ENDPOINT to run the same-play burst simulation.");
            return;
        }
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3-coder:30b";
        IChatClient chat = new OllamaApiClient(new Uri(endpoint), model);
        var composer = new LlmSkillComposer(
            chat, new LlmComposerOptions { Timeout = TimeSpan.FromSeconds(90) }, NullLogger<LlmSkillComposer>.Instance);

        var profile = new List<BehaviorWeight> { new("RangedAttack", 50), new("Dodge", 25) }; // ONE play
        var lineage = new List<PriorArt>();
        var kindSignatures = new List<string>();

        _out.WriteLine($"model: {model} | same play (RangedAttack 50, Dodge 25) fired 5x");
        _out.WriteLine($"{"n",-3} | {"name",-30} | primitive kinds");
        for (int i = 0; i < 5; i++)
        {
            var request = new CompositionRequest(
                "an expedition discovery", new[] { "arcane" }, PrimitiveKind.Beam, new PowerBudget(50),
                Lineage: lineage.TakeLast(4).ToList(), BehaviorProfile: profile, Seed: 5000 + i);

            var outcome = await CompositionPipeline.ForgeAsync(request, composer, maxAttempts: 5);
            Assert.True(outcome.Forged && outcome.Skill is not null, $"burst {i}: deferred.");
            var skill = outcome.Skill!;
            lineage.Add(new PriorArt(skill.Name, skill.Description ?? string.Empty, skill.Primitives));
            var kinds = string.Join(",", skill.Primitives.Select(p => p.Kind).Distinct().OrderBy(k => k.ToString(), StringComparer.Ordinal));
            kindSignatures.Add(kinds);
            _out.WriteLine($"{i,-3} | {skill.Name,-30} | {kinds}");
        }

        int distinct = kindSignatures.Distinct().Count();
        _out.WriteLine($"\ndistinct primitive-kind sets: {distinct}/5");
        // Stress test: IDENTICAL input, so the model can only diversify so far — the retry/
        // avoid loop lifts it well above the ~1/5 it converges to on its own, but not to 5/5.
        // In real play this burst doesn't happen: identical play claims ONCE (the delivery-
        // style key), so distinct claims (which DO differ) are what the retry keeps unique.
        Assert.True(distinct >= 3,
            $"same-play burst barely diversified ({distinct}/5) — the retry/avoid loop is not helping at all.");
    }

    /// <summary>
    /// Mastering the SAME play: the score climbs (harder/longer → higher rarity → higher
    /// budget), each stronger discovery built on the weaker one via the lineage. This is a
    /// real same-play chain ("동일 행동이라도 점수에 따라 달라야 한다") — it must EVOLVE into
    /// richer skills, not reproduce the ancestor. Observes whether rising budget + lineage
    /// grow the composition.
    /// </summary>
    [Fact]
    public async Task SamePlayRisingScore_EvolvesIntoStrongerSkills()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _out.WriteLine("SKIPPED: set OLLAMA_ENDPOINT to run the mastery-chain simulation.");
            return;
        }
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3-coder:30b";
        IChatClient chat = new OllamaApiClient(new Uri(endpoint), model);
        var composer = new LlmSkillComposer(
            chat, new LlmComposerOptions { Timeout = TimeSpan.FromSeconds(90) }, NullLogger<LlmSkillComposer>.Instance);

        var profile = new List<BehaviorWeight> { new("ChargedAttack", 60) }; // one play, mastered harder
        var budgets = new[] { 16, 30, 44, 58, 64 };                          // rising rarity/score
        var lineage = new List<PriorArt>();
        var signatures = new List<string>();
        var sizes = new List<int>();

        _out.WriteLine($"model: {model} | same play (ChargedAttack 60), rising budget + lineage");
        _out.WriteLine($"{"budget",-7} | {"delivery",-9} | {"name",-28} | primitives");
        for (int gen = 0; gen < budgets.Length; gen++)
        {
            var request = new CompositionRequest(
                "an expedition discovery", new[] { "arcane" }, PrimitiveKind.Beam, new PowerBudget(budgets[gen]),
                Lineage: lineage.TakeLast(4).ToList(), BehaviorProfile: profile, Seed: 3000 + gen);

            var outcome = await CompositionPipeline.ForgeAsync(request, composer, maxAttempts: 3);
            Assert.True(outcome.Forged && outcome.Skill is not null,
                $"budget {budgets[gen]}: composition deferred ({outcome.LastValidation.Error}).");

            var skill = outcome.Skill!;
            var prims = string.Join(",", skill.Primitives.Select(p => $"{p.Kind}x{p.Magnitude}"));
            lineage.Add(new PriorArt(skill.Name, skill.Description ?? string.Empty, skill.Primitives));
            signatures.Add(prims);
            sizes.Add(skill.Primitives.Sum(p => p.Magnitude));
            _out.WriteLine($"{budgets[gen],-7} | {skill.Delivery,-9} | {skill.Name,-28} | {prims}");
        }

        int distinct = signatures.Distinct().Count();
        _out.WriteLine($"\ndistinct skills: {distinct}/{budgets.Length} | total magnitude by tier: [{string.Join(", ", sizes)}]");

        // The mastered chain must produce distinct, generally stronger skills — not the same
        // recipe renamed. (Total magnitude should trend up with the budget.)
        Assert.True(distinct >= 4,
            $"same-play mastery chain converged ({distinct}/{budgets.Length}) — rising score didn't evolve the skill; the composition isn't using budget/lineage to grow it.");
        Assert.True(sizes.Last() > sizes.First(),
            $"the strongest tier ({sizes.Last()}) is not stronger than the weakest ({sizes.First()}) — score isn't scaling power.");
    }

    /// <summary>
    /// Manifestation coverage: a high-freedom game, so a discovery isn't always a new weapon.
    /// Magic-offensive play should yield a WEAPON; a non-magic/mobility technique (primary
    /// Dash — "magic + non-magic → a command, not a weapon") should yield a COMMAND; a
    /// defensive lean a PASSIVE. Observes what SkillManifest.Classify produces per play so a
    /// bug (e.g. every discovery becoming a weapon) is caught before playtest.
    /// </summary>
    [Fact]
    public async Task Manifestation_MatchesTheKindOfPlay()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _out.WriteLine("SKIPPED: set OLLAMA_ENDPOINT to run the manifestation simulation.");
            return;
        }
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3-coder:30b";
        IChatClient chat = new OllamaApiClient(new Uri(endpoint), model);
        var composer = new LlmSkillComposer(
            chat, new LlmComposerOptions { Timeout = TimeSpan.FromSeconds(90) }, NullLogger<LlmSkillComposer>.Instance);

        // (name, primary, tags, expected manifestation, play)
        var cases = new (string Name, PrimitiveKind Primary, string[] Tags, ManifestationKind Expect, (string, int)[] Play)[]
        {
            ("magic-charge", PrimitiveKind.Beam, new[] { "arcane" }, ManifestationKind.Weapon, new[] { ("ChargedAttack", 60) }),
            ("magic-rapid", PrimitiveKind.Projectile, new[] { "arcane" }, ManifestationKind.Weapon, new[] { ("RangedAttack", 60) }),
            ("bow-rapid", PrimitiveKind.Projectile, new[] { "bow" }, ManifestationKind.Weapon, new[] { ("RangedAttack", 60) }),
            ("nonmagic-dash", PrimitiveKind.Dash, new[] { "blade", "nonmagic" }, ManifestationKind.Command, new[] { ("MeleeAttack", 25), ("Dodge", 40), ("Jump", 35) }),
            ("nonmagic-mobile", PrimitiveKind.Dash, new[] { "blade", "nonmagic" }, ManifestationKind.Command, new[] { ("Jump", 50), ("Dodge", 45), ("MeleeAttack", 15) }),
            ("guard-lean", PrimitiveKind.Blink, new[] { "ward", "nonmagic" }, ManifestationKind.Command, new[] { ("Dodge", 55), ("Jump", 30) }),
        };

        var results = new List<(string name, ManifestationKind expect, ManifestationKind actual)>();
        _out.WriteLine($"model: {model}");
        _out.WriteLine($"{"play",-16} | {"expect",-8} | {"actual",-8} | {"ok",-4} | primitives");
        foreach (var c in cases)
        {
            var profile = c.Play.Select(p => new BehaviorWeight(p.Item1, p.Item2)).ToList();
            var request = new CompositionRequest(
                "an expedition discovery", c.Tags, c.Primary, new PowerBudget(50),
                Lineage: null, BehaviorProfile: profile, Seed: 4000 + results.Count);

            var outcome = await CompositionPipeline.ForgeAsync(request, composer, maxAttempts: 3);
            Assert.True(outcome.Forged && outcome.Skill is not null, $"{c.Name}: deferred ({outcome.LastValidation.Error}).");
            var skill = outcome.Skill!;
            var actual = SkillManifest.Classify(skill);
            var prims = string.Join(",", skill.Primitives.Select(p => $"{p.Kind}"));
            results.Add((c.Name, c.Expect, actual));
            _out.WriteLine($"{c.Name,-16} | {c.Expect,-8} | {actual,-8} | {(actual == c.Expect ? "ok" : "MISS"),-4} | {prims}");
        }

        int matched = results.Count(r => r.actual == r.expect);
        int distinctKinds = results.Select(r => r.actual).Distinct().Count();
        _out.WriteLine($"\nmanifestation matches: {matched}/{results.Count} | distinct kinds produced: {distinctKinds}");

        // The system must produce more than one manifestation kind (not everything a weapon),
        // and mostly match the intent.
        Assert.True(distinctKinds >= 2,
            $"only {distinctKinds} manifestation kind(s) — non-weapon discoveries (commands) aren't arising; the composition/prompt collapses everything to a weapon.");
        Assert.True(matched >= results.Count - 1,
            $"manifestation mismatch ({matched}/{results.Count}) — observe the MISS rows; the play isn't steering weapon-vs-command.");
    }
}
