using System;
using System.Collections.Generic;
using System.Linq;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// ADR 0005 (재개정) — a command whose combo uses a weapon click (LMB/RMB) is bound to the
    /// weapon category it was discovered with: a "flame + gun" technique can't be reproduced
    /// with a sword. A behaviour-only combo (jump/dodge) is unrestricted. Weapons and passives
    /// are never gated. The check is against the CURRENT loadout, so swapping weapons enables
    /// or disables a command.
    /// </summary>
    public static class CommandGate
    {
        // Only the base weapon categories gate a command; a discovered weapon's own "spell:"
        // tag and the behaviour/situation tags do not.
        private static readonly HashSet<string> Vocabulary =
            new HashSet<string> { EquipmentTags.Melee, EquipmentTags.Firearm, EquipmentTags.Bow, EquipmentTags.Arcane };

        /// <summary>The equipment categories a command requires equipped to be invoked —
        /// empty when it's a behaviour-only combo (no weapon click).</summary>
        public static IReadOnlyCollection<string> RequiredEquipment(DiscoveredSkill command)
        {
            if (command?.Combo == null || command.ContextTags == null) return Array.Empty<string>();
            bool usesWeapon = command.Combo.Any(t => t == InputToken.LeftClick || t == InputToken.RightClick);
            if (!usesWeapon) return Array.Empty<string>();
            return command.ContextTags.Where(Vocabulary.Contains).Distinct().ToList();
        }

        /// <summary>Whether the command can be invoked with the given equipped tags.</summary>
        public static bool Invocable(DiscoveredSkill command, ICollection<string> currentTags)
        {
            var required = RequiredEquipment(command);
            if (required.Count == 0) return true;
            return currentTags != null && required.All(currentTags.Contains);
        }
    }
}
