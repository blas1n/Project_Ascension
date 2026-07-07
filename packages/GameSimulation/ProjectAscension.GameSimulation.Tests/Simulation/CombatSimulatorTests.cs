using System;
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.GameSimulation.Harness;
using Xunit;
using Xunit.Abstractions;

namespace ProjectAscension.GameSimulation.Tests.Simulation
{
    /// <summary>
    /// Mode B: replay simulated fights (a kit of skill graphs vs a dummy wave) headlessly and
    /// assert the combat loop behaves — it terminates, damage is finite/monotonic, an offensive
    /// kit actually clears the wave, a defensive kit doesn't. Coverage the manual playtest can't.
    /// </summary>
    public class CombatSimulatorTests
    {
        private readonly ITestOutputHelper _out;
        public CombatSimulatorTests(ITestOutputHelper output) => _out = output;

        private static EffectNode Cast(params EffectNode[] steps)
            => new Trigger(TriggerKind.OnCast, steps.Length == 1 ? steps[0] : new Sequence(steps));

        [Fact]
        public void AStrongOffensiveKit_ClearsTheWave()
        {
            var kit = new List<EffectNode> { Cast(new Emit(EmitDelivery.Nova, 3), new Damage(3), new Dot(3, 4)) };
            var report = CombatSimulator.Simulate(kit, targetCount: 4, targetHp: 60f);

            Assert.True(report.AllTargetsDown, $"strong AoE kit failed to clear the wave: {report}");
            Assert.True(report.Ticks < 600, "fight did not terminate before the cap");
            _out.WriteLine(report.ToString());
        }

        [Fact]
        public void APurelyDefensiveKit_NeverClearsTheWave()
        {
            var kit = new List<EffectNode> { new Trigger(TriggerKind.Continuous, new Ward(WardEffect.Shield, 3)) };
            var report = CombatSimulator.Simulate(kit, targetCount: 3, targetHp: 40f, maxTicks: 200);

            Assert.False(report.AllTargetsDown);      // no offense → targets survive
            Assert.Equal(0, report.TargetsDown);
            Assert.Equal(0f, report.TotalDamageDealt); // and no damage was dealt
        }

        [Fact]
        public void FuzzedOffensiveKits_AlwaysBehave()
        {
            var rng = new Random(555);
            for (int i = 0; i < 1500; i++)
            {
                var kit = new List<EffectNode> { GraphFuzzer.GenerateOffensive(rng) };
                var report = CombatSimulator.Simulate(kit, targetCount: 3, targetHp: 50f);

                Assert.InRange(report.Ticks, 0, 600);                 // always terminates
                Assert.InRange(report.TargetsDown, 0, report.InitialTargets);
                Assert.True(report.TotalDamageDealt >= 0f && !float.IsNaN(report.TotalDamageDealt)
                    && !float.IsInfinity(report.TotalDamageDealt), $"bad damage total in fight #{i}: {report.TotalDamageDealt}");
                Assert.True(report.PlayerHealed >= 0f && report.PlayerShield >= 0f);
            }
        }

        [Fact]
        public void DamageOverTime_KeepsWorkingTheWaveDown_AfterTheCast()
        {
            // A weak direct hit but a strong lingering burn should still wear a single target down
            // over ticks — proving the DoT stream is applied across the fight, not just on cast.
            var kit = new List<EffectNode> { Cast(new Emit(EmitDelivery.Projectile, 0), new Dot(3, 4)) };
            var report = CombatSimulator.Simulate(kit, targetCount: 1, targetHp: 40f);
            Assert.True(report.AllTargetsDown, $"DoT failed to finish a lone target: {report}");
        }
    }
}
