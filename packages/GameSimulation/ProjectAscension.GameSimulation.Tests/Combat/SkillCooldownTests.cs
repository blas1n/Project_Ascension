using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    /// <summary>Per-skill cooldown (replaces the removed Focus resource) — derived from the
    /// effect graph's power points, clamped to a floor/ceiling, deterministic. Gating mirrors
    /// WeaponFireRulesTests: a clock is passed in, no wall-clock dependency.</summary>
    public class SkillCooldownTests
    {
        private static EffectNode Cast(params EffectNode[] steps)
            => new Trigger(TriggerKind.OnCast, steps.Length == 1 ? steps[0] : new Sequence(steps));

        [Fact]
        public void BiggerSkill_CostsALongerWait()
        {
            var small = Cast(new Emit(EmitDelivery.Projectile, 0));               // 1 point
            var big = Cast(new Emit(EmitDelivery.Nova, 3), new Damage(3), new Dot(3, 4)); // many points
            var tuning = CombatTuning.Default with { CooldownSecondsPerPoint = 1f }; // wide enough to see the difference unclamped

            Assert.True(SkillCooldown.Of(big, tuning) > SkillCooldown.Of(small, tuning));
        }

        [Fact]
        public void EverySkill_LandsWithinFloorAndCeiling()
        {
            var tiny = Cast(new Emit(EmitDelivery.Projectile, 0)); // 1 point × 0.3 = 0.3s → clamped UP to the floor

            // 10 tier-4 Damage nodes = 50 points × 0.3 = 15s → clamped DOWN to the ceiling. A graph
            // this large would never pass the server's power-budget validator (ADR 0010's budget
            // tops out at 40), but the rule must still be safe against whatever reaches it.
            var steps = new EffectNode[10];
            for (int i = 0; i < steps.Length; i++) steps[i] = new Damage(4);
            var massive = Cast(steps);

            Assert.Equal(CombatTuning.Default.CooldownFloorSeconds, SkillCooldown.Of(tiny), precision: 3);
            Assert.Equal(CombatTuning.Default.CooldownCeilingSeconds, SkillCooldown.Of(massive), precision: 3);
        }

        [Fact]
        public void SameGraph_AlwaysYieldsTheSameCooldown()
        {
            var graph = Cast(new Emit(EmitDelivery.Beam, 2), new Damage(1));
            Assert.Equal(SkillCooldown.Of(graph), SkillCooldown.Of(graph), precision: 3);
        }

        [Fact]
        public void CanCast_OnlyOnceTheCooldownElapsed()
        {
            Assert.True(SkillCooldownRules.CanCast(time: 5f, nextReadyTime: 5f));   // exactly ready
            Assert.True(SkillCooldownRules.CanCast(time: 6f, nextReadyTime: 5f));
            Assert.False(SkillCooldownRules.CanCast(time: 4.9f, nextReadyTime: 5f)); // still cooling
        }

        [Fact]
        public void NextReady_IsACooldownAhead()
            => Assert.Equal(5.5f, SkillCooldownRules.NextReady(time: 5f, cooldown: 0.5f), precision: 3);
    }
}
