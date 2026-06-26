using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class SkillResolverTests
    {
        private static Skill Of(params SkillPrimitive[] primitives) => new("Test Skill", primitives);

        [Fact]
        public void Projectile_DamagesPrimaryOnly()
        {
            var res = SkillResolver.Resolve(Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 3)), availableTargets: 3);

            var hit = Assert.Single(res.Hits);
            Assert.Equal(0, hit.TargetIndex);
            Assert.Equal(30f, hit.Damage, precision: 3); // 3 × 10
        }

        [Fact]
        public void Area_DamagesEveryTarget()
        {
            var res = SkillResolver.Resolve(Of(new SkillPrimitive(SkillPrimitiveKind.Area, 2)), availableTargets: 3);

            Assert.Equal(3, res.Hits.Count);
            Assert.All(res.Hits, h => Assert.Equal(16f, h.Damage, precision: 3)); // 2 × 8
        }

        [Fact]
        public void Fork_SpreadsToExtraTargetsWithFalloff()
        {
            var res = SkillResolver.Resolve(
                Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 2), new SkillPrimitive(SkillPrimitiveKind.Fork, 1)),
                availableTargets: 3);

            Assert.Equal(2, res.Hits.Count);        // primary + 1 fork target
            Assert.Equal(20f, res.Hits[0].Damage, precision: 3);
            Assert.Equal(12f, res.Hits[1].Damage, precision: 3); // 20 × 0.6 falloff
        }

        [Fact]
        public void DamageOverTime_ProducesDotStream()
        {
            var res = SkillResolver.Resolve(
                Of(new SkillPrimitive(SkillPrimitiveKind.DamageOverTime, 2, Duration: 1)), availableTargets: 1);

            var hit = Assert.Single(res.Hits);
            Assert.Equal(6f, hit.DamageOverTimePerTick, precision: 3); // 2 × 3
            Assert.Equal(3, hit.DamageOverTimeTicks);                  // 2 base + 1 duration
        }

        [Fact]
        public void Leech_HealsFromDamageDealt()
        {
            var res = SkillResolver.Resolve(
                Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 2), new SkillPrimitive(SkillPrimitiveKind.Leech, 1)),
                availableTargets: 1);

            Assert.Equal(3f, res.SelfHeal, precision: 3); // 20 damage × 0.15
        }

        [Fact]
        public void ShieldAndDash_AreCasterEffectsWithNoDamage()
        {
            var res = SkillResolver.Resolve(
                Of(new SkillPrimitive(SkillPrimitiveKind.Shield, 2), new SkillPrimitive(SkillPrimitiveKind.Dash, 1)),
                availableTargets: 2);

            Assert.Empty(res.Hits);
            Assert.Equal(24f, res.SelfShield, precision: 3); // 2 × 12
            Assert.Equal(2f, res.DashDistance, precision: 3); // 1 × 2
        }

        [Fact]
        public void Control_TakesTheStrongestEffect()
        {
            var res = SkillResolver.Resolve(
                Of(new SkillPrimitive(SkillPrimitiveKind.Projectile, 1),
                   new SkillPrimitive(SkillPrimitiveKind.Slow, 1),
                   new SkillPrimitive(SkillPrimitiveKind.Stun, 1)),
                availableTargets: 1);

            Assert.Equal(ControlKind.Stun, Assert.Single(res.Hits).Control);
        }

        [Fact]
        public void DiscoveredSkill_ResolvesAndAppliesToCombat()
        {
            // The exact primitive format the discovery API returns for "Searing Swarm".
            var skill = SkillParser.Parse("Searing Swarm",
                new[] { "Projectile x2 r1", "Homing x1", "Fork x1", "DamageOverTime x1 d2" });

            var res = SkillResolver.Resolve(skill, availableTargets: 3);

            Assert.Equal(3, res.Hits.Count);                       // projectile + range/fork spread reaches 3
            Assert.Equal(20f, res.Hits[0].Damage, precision: 3);   // primary: 2 × 10
            Assert.Equal(44f, res.ImmediateDamage, precision: 3);  // 20 + 12 + 12
            Assert.All(res.Hits, h => Assert.True(h.DamageOverTimePerTick > 0f)); // burning trail

            // The AI-composed skill actually damages a target.
            var target = CombatResolver.ApplyDamage(Health.Full(100f), res.Hits[0].Damage);
            Assert.Equal(80f, target.Current, precision: 3);
        }
    }
}
