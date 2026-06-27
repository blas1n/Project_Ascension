using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class PassiveTests
    {
        private static DiscoveredSkill Passive(string name, params SkillPrimitive[] primitives)
            => new(name, ManifestationKind.Passive, new Skill(name, primitives));

        [Fact]
        public void Resolve_MapsDefensivePrimitives()
        {
            var effect = PassiveResolver.Resolve(new Skill("Ward",
                new[] { new SkillPrimitive(SkillPrimitiveKind.Barrier, 2), new SkillPrimitive(SkillPrimitiveKind.Leech, 3) }));

            Assert.Equal(0.16f, effect.DamageReduction, precision: 3); // Barrier 2 × 0.08
            Assert.Equal(0.15f, effect.Lifesteal, precision: 3);       // Leech 3 × 0.05
        }

        [Fact]
        public void AggregateDamageReduction_IsCapped()
        {
            // Many strong wards stack but cannot exceed the cap.
            var set = new DiscoveredSkillSet();
            for (int i = 0; i < 5; i++)
                set.Add(Passive($"ward-{i}", new SkillPrimitive(SkillPrimitiveKind.Barrier, 5))); // 0.40 each

            Assert.Equal(PassiveEffect.MaxDamageReduction, set.AggregatePassive().DamageReduction, precision: 3);
        }

        [Fact]
        public void Set_PartitionsAndAggregatesPassives()
        {
            var set = new DiscoveredSkillSet();
            set.Add(Passive("Bulwark", new SkillPrimitive(SkillPrimitiveKind.Barrier, 1)));   // 0.08 reduction
            set.Add(Passive("Siphon", new SkillPrimitive(SkillPrimitiveKind.Leech, 2)));       // 0.10 lifesteal
            set.Add(new DiscoveredSkill("Bolt", ManifestationKind.Weapon,
                new Skill("Bolt", new[] { new SkillPrimitive(SkillPrimitiveKind.Projectile, 1) })));

            Assert.Equal(2, set.Passives.Count);
            Assert.Single(set.Weapons);

            var total = set.AggregatePassive();
            Assert.Equal(0.08f, total.DamageReduction, precision: 3);
            Assert.Equal(0.10f, total.Lifesteal, precision: 3);
        }

        [Fact]
        public void NoPassives_AggregatesToNone()
        {
            Assert.Equal(PassiveEffect.None, new DiscoveredSkillSet().AggregatePassive());
        }
    }
}
