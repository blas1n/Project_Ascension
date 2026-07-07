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
}
