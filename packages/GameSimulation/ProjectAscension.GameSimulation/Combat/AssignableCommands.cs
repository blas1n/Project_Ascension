using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Which discovered commands can be bound to a hotkey RIGHT NOW — filtered by the equipment
    /// gate (ADR 0011: a skill is usable only with the weapons that made it) against the CURRENTLY
    /// equipped loadout. The Equipment Station's hotkey picker lists exactly this set (plus its own
    /// "(none)" entry to clear a slot) rather than every discovered command, so a player with 5+
    /// commands is never offered a bind that would be unusable the moment they closed the panel.
    ///
    /// Pure and headless-tested (ADR: Unity is a shell) — the panel only renders the result.
    /// </summary>
    public static class AssignableCommands
    {
        /// <summary>The commands, in discovery order, usable with <paramref name="equippedTags"/>.</summary>
        public static IReadOnlyList<DiscoveredSkill> For(
            IReadOnlyList<DiscoveredSkill> commands, ICollection<string> equippedTags)
        {
            var result = new List<DiscoveredSkill>();
            if (commands == null) return result;

            foreach (var command in commands)
                if (command != null && SkillBinding.Usable(command.Behaviors, equippedTags))
                    result.Add(command);

            return result;
        }
    }
}
