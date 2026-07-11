using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Seeds the ability hotbar with discovered commands when the player hasn't customised it (ADR:
    /// Unity is a shell). Pure array fill — the first commands map to the first slots, in order,
    /// bounded by both counts — so the starting hotbar is testable without Unity.
    /// </summary>
    public static class CommandSlotDefaults
    {
        /// <summary>Fill <paramref name="slots"/> in place with the first <paramref name="commands"/>,
        /// up to the shorter of the two lengths. A one-time seed; the caller guards re-application.</summary>
        public static void Seed(DiscoveredSkill[] slots, IReadOnlyList<DiscoveredSkill> commands)
        {
            if (slots == null || commands == null) return;
            int n = slots.Length < commands.Count ? slots.Length : commands.Count;
            for (int i = 0; i < n; i++) slots[i] = commands[i];
        }
    }
}
