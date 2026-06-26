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
        public void Use_RequiresTheExactBoundEquipment()
        {
            // Fireball discovered with catalyst + firearm — usable only with that exact
            // pair, hand order irrelevant; not with a subset, superset, or other gear (ADR 0005).
            var skill = new DiscoveredSkill(
                "Fireball", ManifestationKind.Weapon,
                new Skill("Fireball", new[] { new SkillPrimitive(SkillPrimitiveKind.Projectile, 3) }),
                new[] { "arcane", "firearm" });
            var set = new DiscoveredSkillSet();
            set.Add(skill);

            Assert.True(DiscoveredSkillSet.Usable(skill, new HashSet<string> { "firearm", "arcane" })); // exact, any order
            Assert.False(DiscoveredSkillSet.Usable(skill, new HashSet<string> { "firearm" }));            // missing catalyst
            Assert.False(DiscoveredSkillSet.Usable(skill, new HashSet<string> { "firearm", "melee" }));    // wrong second weapon
            Assert.False(DiscoveredSkillSet.Usable(skill, new HashSet<string> { "arcane", "firearm", "bow" })); // extra (no 3rd hand anyway)

            Assert.Equal(30f, set.Use(skill, new HashSet<string> { "arcane", "firearm" }, 1).ImmediateDamage, precision: 3);
            Assert.Equal(0f, set.Use(skill, new HashSet<string> { "firearm", "melee" }, 1).ImmediateDamage, precision: 3); // wrong loadout → no effect
        }

        [Fact]
        public void Use_EmptyBinding_AlwaysUsable()
        {
            // A no-equipment discovery (e.g. a movement technique) has no binding.
            var skill = new DiscoveredSkill(
                "Double Jump", ManifestationKind.Command,
                new Skill("Double Jump", new[] { new SkillPrimitive(SkillPrimitiveKind.Dash, 1) }));
            Assert.True(DiscoveredSkillSet.Usable(skill, new HashSet<string> { "melee" }));
        }
    }
}
