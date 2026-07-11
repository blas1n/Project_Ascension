using System;
using ProjectAscension.GameSimulation.Combat;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Combat
{
    public class CommandSlotDefaultsTests
    {
        private static DiscoveredSkill Cmd(string name)
            => new(name, ManifestationKind.Command, new Skill(name, Array.Empty<SkillPrimitive>()));

        [Fact]
        public void Seed_FillsInOrder_BoundedByBothLengths()
        {
            var slots = new DiscoveredSkill[4];
            var commands = new[] { Cmd("A"), Cmd("B") };

            CommandSlotDefaults.Seed(slots, commands);

            Assert.Equal("A", slots[0].Name);
            Assert.Equal("B", slots[1].Name);
            Assert.Null(slots[2]); // fewer commands than slots — the rest stay empty
            Assert.Null(slots[3]);
        }

        [Fact]
        public void Seed_StopsAtSlotCount_WhenMoreCommandsThanSlots()
        {
            var slots = new DiscoveredSkill[2];
            var commands = new[] { Cmd("A"), Cmd("B"), Cmd("C") };
            CommandSlotDefaults.Seed(slots, commands);
            Assert.Equal("A", slots[0].Name);
            Assert.Equal("B", slots[1].Name); // C doesn't fit
        }

        [Fact]
        public void Seed_NullArgs_AreNoOps()
        {
            CommandSlotDefaults.Seed(null, new[] { Cmd("A") }); // must not throw
            CommandSlotDefaults.Seed(new DiscoveredSkill[2], null);
        }
    }
}
