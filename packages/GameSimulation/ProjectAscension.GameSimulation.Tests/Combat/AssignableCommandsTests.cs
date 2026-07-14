using System;
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class AssignableCommandsTests
    {
        private static DiscoveredSkill Cmd(string name, params string[] behaviors)
            => new(name, ManifestationKind.Command, new Skill(name, Array.Empty<SkillPrimitive>()),
                Behaviors: behaviors);

        [Fact]
        public void ABodyCommand_IsAlwaysAssignable()
        {
            // No weapon named in its behaviours — learned with the body, so nothing to gate on.
            var commands = new[] { Cmd("Phase Step", "Seq:jump>dash") };

            var result = AssignableCommands.For(commands, new HashSet<string>());

            Assert.Equal("Phase Step", Assert.Single(result).Name);
        }

        [Fact]
        public void AWeaponCommand_RequiresThatWeaponEquippedNow()
        {
            var commands = new[]
            {
                Cmd("Piercing Round", "Use:firearm"),
                Cmd("Flame Bullet", "Fuse:arcane>firearm"),
                Cmd("Wide Slash", "Use:melee"),
            };

            var withFirearmAndArcane = AssignableCommands.For(commands, new HashSet<string> { "firearm", "arcane" });
            Assert.Equal(2, withFirearmAndArcane.Count);
            Assert.Contains(withFirearmAndArcane, c => c.Name == "Piercing Round");
            Assert.Contains(withFirearmAndArcane, c => c.Name == "Flame Bullet");

            var withMeleeOnly = AssignableCommands.For(commands, new HashSet<string> { "melee" });
            Assert.Equal("Wide Slash", Assert.Single(withMeleeOnly).Name);
        }

        [Fact]
        public void PreservesDiscoveryOrder()
        {
            var commands = new[] { Cmd("A"), Cmd("B"), Cmd("C") };

            var result = AssignableCommands.For(commands, new HashSet<string>());

            Assert.Equal(new[] { "A", "B", "C" }, new[] { result[0].Name, result[1].Name, result[2].Name });
        }

        [Fact]
        public void NullCommands_ReturnsEmpty()
            => Assert.Empty(AssignableCommands.For(null, new HashSet<string>()));
    }
}
