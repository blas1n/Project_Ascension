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
            var weapon = Weapon(); // no equipment binding → usable with anything
            set.Add(weapon);

            var res = set.Use(weapon, new HashSet<string>(), availableTargets: 1);

            Assert.Equal(30f, res.ImmediateDamage, precision: 3); // Projectile 3 × 10
        }

        [Fact]
        public void Use_RequiresTheBoundEquipment()
        {
            // Discovered with a firearm — usable only while a firearm is equipped (ADR 0005).
            var skill = new DiscoveredSkill(
                "Flame Bolt", ManifestationKind.Weapon,
                new Skill("Flame Bolt", new[] { new SkillPrimitive(SkillPrimitiveKind.Projectile, 3) }),
                new[] { "firearm" });
            var set = new DiscoveredSkillSet();
            set.Add(skill);

            Assert.True(DiscoveredSkillSet.Usable(skill, new HashSet<string> { "firearm" }));
            Assert.False(DiscoveredSkillSet.Usable(skill, new HashSet<string> { "melee" }));

            Assert.Equal(30f, set.Use(skill, new HashSet<string> { "firearm" }, 1).ImmediateDamage, precision: 3);
            Assert.Equal(0f, set.Use(skill, new HashSet<string> { "melee" }, 1).ImmediateDamage, precision: 3); // wrong weapon → no effect
        }
    }
}
