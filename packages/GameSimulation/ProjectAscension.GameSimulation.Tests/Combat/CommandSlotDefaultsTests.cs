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
        public void FillsInDiscoveryOrder_LeavingTheRestEmpty()
        {
            var slots = new DiscoveredSkill[4];

            CommandSlotDefaults.FillFreeSlots(slots, new[] { Cmd("A"), Cmd("B") });

            Assert.Equal("A", slots[0].Name);
            Assert.Equal("B", slots[1].Name);
            Assert.Null(slots[2]);
            Assert.Null(slots[3]);
        }

        [Fact]
        public void ACommandDiscoveredLater_StillReachesAKey()
        {
            // THE bug this replaced: the bar seeded once and locked, so every command found after
            // the first stayed unbound — on a bar with three empty keys.
            var slots = new DiscoveredSkill[4];
            var a = Cmd("A");
            CommandSlotDefaults.FillFreeSlots(slots, new[] { a });

            var flameBullet = Cmd("Flame Bullet"); // found mid-expedition
            bool changed = CommandSlotDefaults.FillFreeSlots(slots, new[] { a, flameBullet });

            Assert.True(changed);
            Assert.Same(a, slots[0]);              // already bound — not moved, not duplicated
            Assert.Equal("Flame Bullet", slots[1].Name);
        }

        [Fact]
        public void RunningAgainWithNothingNew_ChangesNothing()
        {
            var slots = new DiscoveredSkill[2];
            var commands = new[] { Cmd("A"), Cmd("B") };
            CommandSlotDefaults.FillFreeSlots(slots, commands);

            Assert.False(CommandSlotDefaults.FillFreeSlots(slots, commands)); // idempotent
        }

        [Fact]
        public void AKeyThePlayerSet_IsNeverAutoFilled_EvenWhenEmptied()
        {
            // The player cleared slot 0 on purpose. Auto-fill must not undo that.
            var slots = new DiscoveredSkill[2];
            var playerSet = new[] { true, false };

            CommandSlotDefaults.FillFreeSlots(slots, new[] { Cmd("A"), Cmd("B") }, playerSet);

            Assert.Null(slots[0]);
            Assert.Equal("A", slots[1].Name);
        }

        [Fact]
        public void AFullBar_KeepsTheOverflowUnbound()
        {
            var slots = new DiscoveredSkill[2];
            CommandSlotDefaults.FillFreeSlots(slots, new[] { Cmd("A"), Cmd("B"), Cmd("C") });

            Assert.Equal("A", slots[0].Name);
            Assert.Equal("B", slots[1].Name); // C doesn't fit — the player rearranges
        }

        [Fact]
        public void NullArgs_AreNoOps()
        {
            Assert.False(CommandSlotDefaults.FillFreeSlots(null, new[] { Cmd("A") }));
            Assert.False(CommandSlotDefaults.FillFreeSlots(new DiscoveredSkill[2], null));
        }
    }
}
