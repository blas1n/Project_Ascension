using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Keeps the ability hotbar filled with discovered commands (ADR: Unity is a shell).
    ///
    /// A command you just discovered has to reach a key the MOMENT it exists — the discovery
    /// happened mid-expedition and that is exactly when you want to use it. So this is idempotent
    /// and runs continuously, rather than seeding once and locking (which left every command found
    /// after the first one unbound, on a bar with three empty keys).
    /// </summary>
    public static class CommandSlotDefaults
    {
        /// <summary>
        /// Bind every not-yet-bound command to a free slot, in discovery order. Never overwrites an
        /// occupied slot, and never touches a slot the player set themselves (<paramref name="playerSet"/>)
        /// — including one they deliberately emptied. Returns whether anything changed.
        /// </summary>
        public static bool FillFreeSlots(DiscoveredSkill[] slots, IReadOnlyList<DiscoveredSkill> commands,
            IReadOnlyList<bool> playerSet = null)
        {
            if (slots == null || commands == null) return false;

            bool changed = false;
            foreach (var command in commands)
            {
                if (command == null || Bound(slots, command)) continue;

                int free = FreeSlot(slots, playerSet);
                if (free < 0) break; // the bar is full — the rest is the player's to arrange

                slots[free] = command;
                changed = true;
            }
            return changed;
        }

        private static bool Bound(DiscoveredSkill[] slots, DiscoveredSkill command)
        {
            foreach (var slot in slots)
                if (ReferenceEquals(slot, command)) return true;
            return false;
        }

        private static int FreeSlot(DiscoveredSkill[] slots, IReadOnlyList<bool> playerSet)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) continue;
                if (playerSet != null && i < playerSet.Count && playerSet[i]) continue;
                return i;
            }
            return -1;
        }
    }
}
