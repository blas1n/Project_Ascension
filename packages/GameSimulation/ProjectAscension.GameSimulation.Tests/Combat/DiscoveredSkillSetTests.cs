using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class DiscoveredSkillSetTests
    {
        private static DiscoveredSkill Weapon() => new(
            "Flame Bolt", ManifestationKind.Weapon,
            new Skill("Flame Bolt", new[] { new SkillPrimitive(SkillPrimitiveKind.Projectile, 3) }));

        private static DiscoveredSkill Command() => new(
            "Phase Step", ManifestationKind.Command,
            new Skill("Phase Step", new[] { new SkillPrimitive(SkillPrimitiveKind.Dash, 2) }));

        [Fact]
        public void Partitions_WeaponsAndCommands()
        {
            var set = new DiscoveredSkillSet();
            set.Add(Weapon());
            set.Add(Command());

            Assert.Equal("Flame Bolt", Assert.Single(set.Weapons).Name);
            Assert.Equal("Phase Step", Assert.Single(set.Commands).Name);
        }

        [Fact]
        public void Use_ResolvesThroughTheResolver()
        {
            var set = new DiscoveredSkillSet();
            var weapon = Weapon();
            set.Add(weapon);

            var res = set.Use(weapon, availableTargets: 1);

            Assert.Equal(30f, res.ImmediateDamage, precision: 3); // Projectile 3 × 10
        }
    }
}
