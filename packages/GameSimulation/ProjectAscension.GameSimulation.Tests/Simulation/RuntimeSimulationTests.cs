using System;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.GameSimulation.Harness;
using Xunit;
using Xunit.Abstractions;

namespace ProjectAscension.GameSimulation.Tests.Simulation
{
    /// <summary>
    /// Headless runtime simulation (ADR 0007) — the deterministic answer to "a manual playtest
    /// can't cover every situation". It fuzzes thousands of effect graphs across the whole
    /// vocabulary and runs each through the real runtime interpreters (GraphSkillResolver,
    /// MovementCapability, PlayerSimulation), asserting the runtime invariants hold. Seeded, so
    /// any failure is reproducible. Runs in CI (no LLM, no Unity).
    /// </summary>
    public class RuntimeSimulationTests
    {
        private readonly ITestOutputHelper _out;
        public RuntimeSimulationTests(ITestOutputHelper output) => _out = output;

        [Fact]
        public void Offensive_Resolution_HoldsInvariants_AcrossManyGraphsAndTargetCounts()
        {
            var rng = new Random(1234);
            int checkedGraphs = 0;
            for (int i = 0; i < 3000; i++)
            {
                var graph = GraphFuzzer.GenerateOffensive(rng);
                // Resolve against a spread of crowd sizes, including the 0-target edge.
                for (int targets = 0; targets <= 6; targets++)
                {
                    var res = GraphSkillResolver.Resolve(graph, targets, CombatTuning.Default);
                    var violation = RuntimeInvariants.CheckResolution(res, targets);
                    Assert.True(violation is null,
                        $"seed graph #{i} @ {targets} targets violated: {violation}\n{EffectGraphJson(graph)}");
                    checkedGraphs++;
                }
            }
            _out.WriteLine($"offensive: {checkedGraphs} (graph × target-count) resolutions, all invariant-clean.");
        }

        [Fact]
        public void AnyGraph_NeverThrows_WhenResolvedAsCombat()
        {
            // Even non-offensive / malformed-mix graphs must resolve without throwing — the client
            // executes whatever it's handed; the server owns validation.
            var rng = new Random(99);
            for (int i = 0; i < 3000; i++)
            {
                var graph = GraphFuzzer.Generate(rng);
                var ex = Record.Exception(() => GraphSkillResolver.Resolve(graph, availableTargets: 4, CombatTuning.Default));
                Assert.True(ex is null, $"resolve threw on graph #{i}: {ex}");
            }
        }

        [Fact]
        public void Movement_HoldsInvariants_AcrossManyGraphs()
        {
            var rng = new Random(2468);
            for (int i = 0; i < 4000; i++)
            {
                var graph = GraphFuzzer.Generate(rng); // any trigger, so most grant no movement
                var m = RuntimeInvariants.CheckMovement(graph);
                Assert.True(m is null, $"movement invariant on graph #{i}: {m}");
                var j = RuntimeInvariants.CheckAirJumpBudget(graph);
                Assert.True(j is null, $"air-jump budget on graph #{i}: {j}");
            }
        }

        [Fact]
        public void MovementGraphs_GrantBoundedCapabilities()
        {
            var rng = new Random(13);
            for (int i = 0; i < 2000; i++)
            {
                var graph = GraphFuzzer.GenerateMovement(rng);
                Assert.Null(RuntimeInvariants.CheckMovement(graph));
                Assert.Null(RuntimeInvariants.CheckAirJumpBudget(graph));
            }
        }

        private static string EffectGraphJson(EffectNode graph)
            => graph is Trigger t ? $"Trigger({t.Kind}, …)" : graph?.GetType().Name ?? "null";
    }
}
