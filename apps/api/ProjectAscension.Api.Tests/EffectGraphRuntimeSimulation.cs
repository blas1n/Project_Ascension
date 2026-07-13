using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OllamaSharp;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.GameSimulation.Harness;
using Xunit;
using Xunit.Abstractions;
using SF = ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Tests;

/// <summary>
/// Runtime simulation, mode C (ADR 0007) — the bridge from GENERATION to EXECUTION. The real LLM
/// composes each skill as a graph (JSON); we parse it with the CLIENT reader and run it through the
/// SAME runtime the game uses (GraphSkillResolver / MovementCapability / CombatSimulator), then
/// assert the runtime invariants hold and offensive skills actually fight. So we don't just check
/// the AI produces valid-looking graphs — we check the graphs it produces BEHAVE when executed.
///
/// Gated on OLLAMA_ENDPOINT so CI skips it. Run on demand:
///   OLLAMA_ENDPOINT=http://host:11434 dotnet test --filter FullyQualifiedName~EffectGraphRuntimeSimulation
/// </summary>
public class EffectGraphRuntimeSimulation
{
    private readonly ITestOutputHelper _out;
    public EffectGraphRuntimeSimulation(ITestOutputHelper output) => _out = output;

    [Fact]
    public async Task UnifiedComposition_ProducesNameDescriptionAndGraph()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _out.WriteLine("SKIPPED: set OLLAMA_ENDPOINT to run the unified-composition check.");
            return;
        }
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3-coder:30b";
        IChatClient chat = new OllamaApiClient(new Uri(endpoint), model);
        var budget = new SF.PowerBudget(60);

        var cases = new (string Name, string Theme, (string, int)[] Play)[]
        {
            ("fire", "a searing fire bolt", new[] { ("RangedAttack", 60) }),
            ("frost", "a chilling frost nova", new[] { ("ChargedAttack", 55) }),
            ("leap", "a nimble aerial hop", new[] { ("Jump", 70) }),
        };
        int ok = 0, seed = 7500;
        foreach (var c in cases)
        {
            var profile = c.Play.Select(p => new SF.BehaviorWeight(p.Item1, p.Item2)).ToList();
            var prompt = SF.SkillGraphPrompt.Build(c.Theme, profile, budget);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var response = await chat.GetResponseAsync(prompt,
                new ChatOptions { ResponseFormat = ChatResponseFormat.Json, Seed = seed++, Temperature = 0.7f }, cts.Token);

            var comp = SF.EffectGraphJson.ParseComposition(response.Text);
            bool good = comp is not null && !string.IsNullOrWhiteSpace(comp.Name)
                && !string.IsNullOrWhiteSpace(comp.Description)
                && SF.EffectGraphValidator.Validate(comp.Graph, budget).IsValid;
            if (good) ok++;
            _out.WriteLine($"{c.Name,-6} | {(good ? "ok" : "BAD"),-4} | name={comp?.Name} | {(comp is not null ? SF.EffectGraphJson.Serialize(comp.Graph) : response.Text)}");
        }
        Assert.True(ok >= cases.Length - 1, $"unified composition failed to yield name+description+graph ({ok}/{cases.Length}).");
    }

    [Fact]
    public async Task AiGraphs_ExecuteCleanly_AndOffensiveOnesFight()
    {
        var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _out.WriteLine("SKIPPED: set OLLAMA_ENDPOINT to run the runtime bridge simulation.");
            return;
        }
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3-coder:30b";
        IChatClient chat = new OllamaApiClient(new Uri(endpoint), model);
        var budget = new SF.PowerBudget(60);

        var cases = new (string Name, string Theme, (string, int)[] Play, bool Offensive)[]
        {
            ("fire-bolt",   "a searing fire bolt that lingers",  new[] { ("RangedAttack", 60) }, true),
            ("frost-nova",  "a frost nova that chills all",      new[] { ("ChargedAttack", 55) }, true),
            ("chain-arc",   "lightning that leaps between foes", new[] { ("RangedAttack", 50) }, true),
            ("double-jump", "a nimble aerial hop",               new[] { ("Jump", 70) }, false),
            ("wall-climb",  "scaling a sheer cliff face",        new[] { ("Jump", 85) }, false),
        };

        int parsed = 0, cleanRuntime = 0, offensiveFought = 0, offensiveCases = 0;
        int seed = 7300;
        _out.WriteLine($"model: {model}");
        _out.WriteLine($"{"case",-12} | {"parsed",-6} | {"invariants",-10} | {"fought",-6} | graph");

        foreach (var c in cases)
        {
            var profile = c.Play.Select(p => new SF.BehaviorWeight(p.Item1, p.Item2)).ToList();
            var prompt = SF.EffectGraphPrompt.Build(c.Theme, profile, budget);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var response = await chat.GetResponseAsync(
                prompt,
                new ChatOptions { ResponseFormat = ChatResponseFormat.Json, Seed = seed++, Temperature = 0.7f },
                cts.Token);

            var graph = EffectGraphReader.Parse(response.Text);
            if (graph is null)
            {
                _out.WriteLine($"{c.Name,-12} | {"NO",-6} | {"-",-10} | {"-",-6} | {response.Text}");
                continue;
            }
            parsed++;

            // Runtime invariants: resolves cleanly across crowd sizes, movement is bounded.
            string violation = RuntimeInvariants.CheckMovement(graph) ?? RuntimeInvariants.CheckAirJumpBudget(graph);
            for (int targets = 0; targets <= 5 && violation is null; targets++)
                violation = RuntimeInvariants.CheckResolution(
                    GraphSkillResolver.Resolve(graph, targets, CombatTuning.Default), targets);
            bool clean = violation is null;
            if (clean) cleanRuntime++;

            bool fought = false;
            if (c.Offensive)
            {
                offensiveCases++;
                // Replay a short fight; an offensive graph should deal damage over the wave.
                var report = CombatSimulator.Simulate(new List<EffectNode> { graph }, targetCount: 3, targetHp: 50f);
                fought = report.TotalDamageDealt > 0f;
                if (fought) offensiveFought++;
            }

            _out.WriteLine($"{c.Name,-12} | {"yes",-6} | {(clean ? "clean" : violation),-10} | {(c.Offensive ? (fought ? "yes" : "NO") : "-"),-6} | delivery={EffectGraphQuery.DeliveryStyle(graph)}");
        }

        _out.WriteLine($"\nparsed: {parsed}/{cases.Length} | runtime-clean: {cleanRuntime}/{parsed} | offensive fought: {offensiveFought}/{offensiveCases}");

        Assert.True(parsed >= cases.Length - 1, $"AI graphs didn't parse ({parsed}/{cases.Length}).");
        Assert.Equal(parsed, cleanRuntime); // every parsed graph must execute invariant-clean
        Assert.True(offensiveFought >= offensiveCases - 1,
            $"offensive AI graphs didn't deal damage in the sim ({offensiveFought}/{offensiveCases}) — generation and execution disagree.");
    }
}
