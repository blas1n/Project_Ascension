using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OllamaSharp;
using ProjectAscension.SkillForge;
using Xunit;
using Xunit.Abstractions;

namespace ProjectAscension.Api.Tests;

/// <summary>
/// Simulation for the effect-graph DSL (ADR 0007): drives synthetic play sessions through the
/// real LLM, which composes each skill as an effect GRAPH (JSON), then parses + validates it.
/// Asserts the AI produces VALID, structurally VARIED graphs and that the STRUCTURE follows the
/// play — offensive play → OnCast, movement play → a movement trigger — including NOVEL movement
/// (a wall-climb theme can reach OnWallContact with no engine change). This is how we'll keep
/// checking the generative DSL instead of hand-testing in Unity.
///
/// Gated on OLLAMA_ENDPOINT so CI skips it. Run on demand:
///   OLLAMA_ENDPOINT=http://host:11434 dotnet test --filter FullyQualifiedName~EffectGraphSimulation
/// </summary>
public class EffectGraphSimulation
{
    private readonly ITestOutputHelper _out;
    public EffectGraphSimulation(ITestOutputHelper output) => _out = output;

    private static readonly TriggerKind[] Movement =
        { TriggerKind.OnJumpInAir, TriggerKind.OnDodge, TriggerKind.OnWallContact };

    [Fact]
    public async Task AiComposesValidVariedGraphs_StructureFollowsPlay()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _out.WriteLine("SKIPPED: set OLLAMA_ENDPOINT to run the effect-graph simulation.");
            return;
        }
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3-coder:30b";
        IChatClient chat = new OllamaApiClient(new Uri(endpoint), model);
        var budget = new PowerBudget(50);

        // (name, theme, play, isMovementPlay)
        var cases = new (string Name, string Theme, (string, int)[] Play, bool Movement)[]
        {
            ("magic-charge", "an arcane charged blast", new[] { ("ChargedAttack", 60) }, false),
            ("bow-rapid",    "a rapid arrow volley",    new[] { ("RangedAttack", 60) }, false),
            ("double-jump",  "a nimble aerial hop",     new[] { ("Jump", 55), ("Dodge", 20) }, true),
            ("wall-climb",   "scaling a sheer cliff face", new[] { ("Jump", 40), ("Dodge", 45) }, true),
            ("guard",        "an enduring protective ward", new[] { ("Dodge", 30) }, false),
        };

        var triggers = new List<TriggerKind>();
        int valid = 0, movementMatched = 0, movementCases = 0;
        _out.WriteLine($"model: {model}");
        _out.WriteLine($"{"case",-13} | {"valid",-5} | {"trigger",-14} | graph");
        int seed = 7000;
        foreach (var c in cases)
        {
            var profile = c.Play.Select(p => new BehaviorWeight(p.Item1, p.Item2)).ToList();
            var prompt = EffectGraphPrompt.Build(c.Theme, profile, budget);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var response = await chat.GetResponseAsync(
                prompt,
                new ChatOptions { ResponseFormat = ChatResponseFormat.Json, Seed = seed++, Temperature = 0.7f },
                cts.Token);

            var graph = EffectGraphJson.Parse(response.Text);
            bool ok = graph is not null && EffectGraphValidator.Validate(graph, budget).IsValid;
            if (ok) valid++;

            var trig = graph is Trigger t ? t.Kind : (TriggerKind?)null;
            if (trig.HasValue) triggers.Add(trig.Value);
            if (c.Movement)
            {
                movementCases++;
                if (trig.HasValue && Movement.Contains(trig.Value)) movementMatched++;
            }

            _out.WriteLine($"{c.Name,-13} | {(ok ? "ok" : "BAD"),-5} | {(trig?.ToString() ?? "-"),-14} | {(graph is not null ? EffectGraphJson.Serialize(graph) : response.Text)}");
        }

        int distinctTriggers = triggers.Distinct().Count();
        _out.WriteLine($"\nvalid: {valid}/{cases.Length} | distinct triggers: {distinctTriggers} | movement→movement-trigger: {movementMatched}/{movementCases}");

        Assert.True(valid >= cases.Length - 1,
            $"AI produced invalid graphs ({valid}/{cases.Length}) — see BAD rows; the DSL/prompt isn't yielding parseable, in-budget structure.");
        Assert.True(distinctTriggers >= 3,
            $"only {distinctTriggers} distinct trigger(s) — the graph STRUCTURE isn't varying with the play.");
        Assert.True(movementMatched >= 1,
            "no movement play produced a movement trigger — the structure isn't following the play (double jump / wall-climb should not be OnCast).");
    }

    /// <summary>
    /// Phase 4b gate: with the offensive vocabulary expanded (Dot/Spread/Homing on top of
    /// Emit/Damage/Control), the AI's OFFENSIVE graphs must be as VARIED as the old primitive
    /// skills — different deliveries AND real use of the riders, not Emit+Damage every time. If
    /// this holds, migrating combat to the graph doesn't flatten variety.
    /// </summary>
    [Fact]
    public async Task AiComposesDiverseOffensiveGraphs()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _out.WriteLine("SKIPPED: set OLLAMA_ENDPOINT to run the offensive-diversity simulation.");
            return;
        }
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3-coder:30b";
        IChatClient chat = new OllamaApiClient(new Uri(endpoint), model);
        var budget = new PowerBudget(60);

        var cases = new (string Name, string Theme, (string, int)[] Play)[]
        {
            ("fire-bolt",    "a searing fire bolt that lingers",        new[] { ("RangedAttack", 60) }),
            ("frost-nova",   "a frost nova that chills all around",     new[] { ("ChargedAttack", 55) }),
            ("chain-arc",    "lightning that leaps between foes",       new[] { ("RangedAttack", 50) }),
            ("seeking-hex",  "a homing hex that hunts its target",      new[] { ("RangedAttack", 45) }),
            ("piercing-lance", "a lance that skewers a whole line",     new[] { ("MeleeAttack", 50) }),
            ("shatter-beam", "a beam that stuns then shatters",         new[] { ("ChargedAttack", 55) }),
        };

        var deliveries = new List<EmitDelivery>();
        var riderKinds = new HashSet<string>();
        int valid = 0, seed = 7100;
        _out.WriteLine($"model: {model}");
        _out.WriteLine($"{"case",-15} | {"valid",-5} | graph");
        foreach (var c in cases)
        {
            var profile = c.Play.Select(p => new BehaviorWeight(p.Item1, p.Item2)).ToList();
            var prompt = EffectGraphPrompt.Build(c.Theme, profile, budget);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var response = await chat.GetResponseAsync(
                prompt,
                new ChatOptions { ResponseFormat = ChatResponseFormat.Json, Seed = seed++, Temperature = 0.7f },
                cts.Token);

            var graph = EffectGraphJson.Parse(response.Text);
            bool ok = graph is not null && EffectGraphValidator.Validate(graph, budget).IsValid;
            if (ok) valid++;
            if (graph is not null)
            {
                CollectNodes(graph, deliveries, riderKinds);
                _out.WriteLine($"{c.Name,-15} | {(ok ? "ok" : "BAD"),-5} | {EffectGraphJson.Serialize(graph)}");
            }
            else
            {
                _out.WriteLine($"{c.Name,-15} | {"BAD",-5} | {response.Text}");
            }
        }

        int distinctDeliveries = deliveries.Distinct().Count();
        // How many of the expressive RIDERS the AI actually reached for across the set.
        var expressiveRiders = new[] { "Dot", "Spread", "Homing", "Control" };
        int ridersUsed = expressiveRiders.Count(riderKinds.Contains);
        _out.WriteLine($"\nvalid: {valid}/{cases.Length} | distinct deliveries: {distinctDeliveries} | expressive riders used: {ridersUsed}/{expressiveRiders.Length} ({string.Join(",", riderKinds)})");

        Assert.True(valid >= cases.Length - 1,
            $"AI produced invalid offensive graphs ({valid}/{cases.Length}).");
        Assert.True(distinctDeliveries >= 3,
            $"only {distinctDeliveries} distinct deliveries — offensive shape isn't varying (expected projectile/beam/burst/nova mix).");
        Assert.True(ridersUsed >= 3,
            $"only {ridersUsed}/4 expressive riders used — the expanded vocabulary (Dot/Spread/Homing/Control) isn't being exercised, so combat variety would regress.");
    }

    // Collect every Emit delivery + the set of node-kind names present, walking the graph.
    private static void CollectNodes(EffectNode node, List<EmitDelivery> deliveries, HashSet<string> kinds)
    {
        switch (node)
        {
            case Trigger t: CollectNodes(t.Child, deliveries, kinds); break;
            case Sequence s: foreach (var step in s.Steps) CollectNodes(step, deliveries, kinds); break;
            case Emit e: deliveries.Add(e.Delivery); kinds.Add("Emit"); break;
            case Damage: kinds.Add("Damage"); break;
            case Dot: kinds.Add("Dot"); break;
            case Spread: kinds.Add("Spread"); break;
            case Homing: kinds.Add("Homing"); break;
            case Control: kinds.Add("Control"); break;
            case Ward: kinds.Add("Ward"); break;
            case Impulse: kinds.Add("Impulse"); break;
        }
    }
}
