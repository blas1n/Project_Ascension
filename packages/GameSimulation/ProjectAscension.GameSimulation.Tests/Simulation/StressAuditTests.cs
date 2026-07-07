using System;
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.GameSimulation.Harness;
using Xunit;
using Xunit.Abstractions;
using EffectSpread = ProjectAscension.GameSimulation.Effects.Spread;

namespace ProjectAscension.GameSimulation.Tests.Simulation
{
    /// <summary>
    /// A design/balance AUDIT driven by the simulation — the properties that should hold for the
    /// graph combat to feel right, which passing unit tests don't assert. Reports the numbers (so
    /// they can be eyeballed) and fails when a design invariant breaks.
    /// </summary>
    public class StressAuditTests
    {
        private readonly ITestOutputHelper _out;
        public StressAuditTests(ITestOutputHelper output) => _out = output;

        private static EffectNode Cast(params EffectNode[] steps)
            => new Trigger(TriggerKind.OnCast, steps.Length == 1 ? steps[0] : new Sequence(steps));

        [Fact]
        public void Tier_MonotonicallyIncreasesOffensivePower()
        {
            // A higher-tier version of the same skill must not deal LESS damage — otherwise budget
            // and rarity are meaningless on the graph combat path.
            float prev = -1f;
            var line = new List<string>();
            for (int tier = 0; tier <= EffectGraphMaxTier(); tier++)
            {
                var kit = new List<EffectNode> { Cast(new Emit(EmitDelivery.Projectile, tier), new Damage(tier)) };
                // Big HP so nothing dies — measure raw single-cast output via one beat.
                var report = CombatSimulator.Simulate(kit, targetCount: 1, targetHp: 100000f, maxTicks: 1, castIntervalTicks: 1);
                line.Add($"t{tier}={report.TotalDamageDealt:F0}");
                Assert.True(report.TotalDamageDealt >= prev,
                    $"tier {tier} dealt {report.TotalDamageDealt} < tier {tier - 1}'s {prev} — power not monotonic in tier");
                prev = report.TotalDamageDealt;
            }
            _out.WriteLine("single-cast damage by tier: " + string.Join(", ", line));
        }

        [Fact]
        public void HigherTierKits_ClearWavesNoSlower()
        {
            // Averaged over fuzzed shapes, a tier-3 kit should clear a wave in no more ticks than a
            // tier-0 kit — a coarse balance sanity on the whole vocabulary, not one skill.
            int lowTicks = AverageClearTicks(tier: 0, seed: 11);
            int highTicks = AverageClearTicks(tier: 3, seed: 11);
            _out.WriteLine($"avg clear ticks — tier0: {lowTicks}, tier3: {highTicks}");
            Assert.True(highTicks <= lowTicks,
                $"tier-3 kits cleared slower ({highTicks}) than tier-0 ({lowTicks}) — tier scaling is inverted/flat");
        }

        [Fact]
        public void DeadSkillRate_AmongEmittingGraphs_IsLow()
        {
            // A graph that EMITS should deal damage. Count how many emitting offensive graphs deal
            // zero — a high rate would mean the interpreter drops output (a real bug), not design.
            var rng = new Random(4242);
            int emitting = 0, deadEmitting = 0, controlOnly = 0;
            for (int i = 0; i < 3000; i++)
            {
                var graph = GraphFuzzer.GenerateOffensive(rng); // always has ≥1 Emit
                emitting++;
                var res = GraphSkillResolver.Resolve(graph, availableTargets: 3, CombatTuning.Default);
                if (res.ImmediateDamage <= 0f)
                {
                    // An Emit with tier 0 + no Damage still yields (0+1)*ProjectileDamage > 0, so a
                    // zero here means the interpreter produced no damage despite emitting.
                    bool anyDot = false;
                    foreach (var h in res.Hits) if (h.DamageOverTimePerTick > 0f) anyDot = true;
                    if (!anyDot) deadEmitting++;
                }
            }
            _out.WriteLine($"emitting graphs: {emitting}, dead (emit but 0 damage & 0 dot): {deadEmitting}, control-only carve-outs: {controlOnly}");
            Assert.Equal(0, deadEmitting); // emitting must always produce some damage
        }

        [Fact]
        public void ManifestationlessAudit_MovementNeverDealsCombatDamage()
        {
            // A movement graph (impulse under a movement trigger) must not leak into combat damage —
            // it should resolve to no hits, so it can't double as a weapon.
            var rng = new Random(7);
            for (int i = 0; i < 2000; i++)
            {
                var graph = GraphFuzzer.GenerateMovement(rng);
                var res = GraphSkillResolver.Resolve(graph, availableTargets: 4, CombatTuning.Default);
                Assert.Equal(0f, res.ImmediateDamage);
            }
        }

        private static int AverageClearTicks(int tier, int seed)
        {
            var rng = new Random(seed);
            long total = 0; int runs = 400;
            for (int i = 0; i < runs; i++)
            {
                // Same fuzzed SHAPE at the given tier: rebuild an offensive graph then flatten tiers.
                var graph = ForceTier(GraphFuzzer.GenerateOffensive(rng), tier);
                var report = CombatSimulator.Simulate(new List<EffectNode> { graph }, targetCount: 3, targetHp: 80f);
                total += report.Ticks;
            }
            return (int)(total / runs);
        }

        // Rebuild a graph with every node's tier forced to a fixed value — isolates tier from shape.
        private static EffectNode ForceTier(EffectNode node, int tier) => node switch
        {
            Trigger t => new Trigger(t.Kind, ForceTier(t.Child, tier)),
            Sequence s => new Sequence(Map(s.Steps, tier)),
            Emit e => new Emit(e.Delivery, tier),
            Damage => new Damage(tier),
            Dot d => new Dot(tier, d.Duration),
            EffectSpread => new EffectSpread(tier),
            Homing => new Homing(tier),
            Control c => new Control(c.Effect, tier),
            Ward w => new Ward(w.Effect, tier),
            Impulse i => new Impulse(i.Direction, tier),
            _ => node,
        };

        private static List<EffectNode> Map(IReadOnlyList<EffectNode> steps, int tier)
        {
            var outp = new List<EffectNode>(steps.Count);
            foreach (var s in steps) outp.Add(ForceTier(s, tier));
            return outp;
        }

        private static int EffectGraphMaxTier() => 3;
    }
}
