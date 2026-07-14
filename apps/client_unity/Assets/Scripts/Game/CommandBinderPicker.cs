using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The picker for binding a discovered COMMAND to an ability hotkey — shared by the Equipment
    /// Station's ability bar and the discovery journal's own binder. Binding a command is knowledge,
    /// not equipment: the journal binds it anywhere (city or frontier), while the station keeps its
    /// own copy for players who are already there. Extracted so the two callers can't drift: both
    /// list only the commands ASSIGNABLE with the CURRENT loadout (GameSimulation.Combat.
    /// AssignableCommands, ADR 0011), the same "(none)" clear entry, the same "already bound
    /// elsewhere" label, and the same combat-lock rule (BindingRules) — binding is knowledge you
    /// carry everywhere, but not while a monster is actively working you over.
    ///
    /// Pure UI — the actual bind is the caller's call (GameSession.AssignCommandSlot).
    /// </summary>
    public static class CommandBinderPicker
    {
        /// <summary>Shown when the binder is disabled — the ONE reason, so both callers say the
        /// exact same thing rather than drifting into two phrasings for the same rule.</summary>
        public const string LockedReason = "Not while you are fighting";

        public static string Title(int slotIndex) => $"Bind a command to [{AbilitySlots.SlotLabel(slotIndex)}]";

        /// <summary>Whether the player may rebind right now — GameSimulation.Combat.BindingRules'
        /// decision, fed by CombatActivityClock's clock (the shell's record of the player's own last
        /// combat activity) and the DB-driven lock window (CombatTuningCatalog).</summary>
        public static bool CanRebindNow()
            => BindingRules.CanRebind(
                CombatActivityClock.LastCombatTime, Time.time, CombatTuningCatalog.Current.BindingCombatLockSeconds);

        /// <summary>The equipment tags the picker filters against — the SELECTION (PlayerState),
        /// not a scene Loadout lookup: SetLeft/SetRight already re-equip on the spot (see
        /// PlayerStateService), so these agree, and this reads correctly even before a scene's
        /// Loadout component exists (e.g. the journal, which works in either scene).</summary>
        public static HashSet<string> CurrentEquipmentTags(PlayerStateService ps)
        {
            var tags = new HashSet<string>();
            var left = EquipmentTags.For(ps.SelectedLeft);
            var right = EquipmentTags.For(ps.SelectedRight);
            if (left != null) tags.Add(left);
            if (right != null) tags.Add(right);
            return tags;
        }

        /// <summary>Draws the picker's entries ("(none)" to clear, then every assignable command)
        /// into the caller's already-open GUILayout scroll view. Returns true the frame a choice was
        /// made — <paramref name="selection"/> is the command to bind, or null to clear the slot.
        /// Re-checks <see cref="CanRebindNow"/> on every call: if combat started WHILE the picker
        /// was already open (the player opened it clean, then took a hit before clicking), the list
        /// closes itself out from under the click rather than letting a bind land mid-fight — the
        /// same invariant the caller enforces by never letting the picker OPEN locked.</summary>
        public static bool DrawEntries(
            GameSession session, HashSet<string> equippedTags, int slotIndex, out DiscoveredSkill selection)
        {
            selection = null;

            if (!CanRebindNow())
            {
                GUILayout.Label($"  {LockedReason}");
                return false;
            }

            if (GUILayout.Button("(none)", GUILayout.Height(28)))
                return true; // selection stays null -> clear

            var assignable = AssignableCommands.For(session.DiscoveredSkills.Commands, equippedTags);
            if (assignable.Count == 0)
            {
                GUILayout.Label("  (no commands usable with your current loadout)");
                return false;
            }

            foreach (var command in assignable)
            {
                int boundSlot = session.SlotOf(command);
                string label = boundSlot >= 0 && boundSlot != slotIndex
                    ? $"{command.Name}  (currently [{AbilitySlots.SlotLabel(boundSlot)}])"
                    : command.Name;
                if (!GUILayout.Button(label, GUILayout.Height(28))) continue;
                selection = command;
                return true;
            }
            return false;
        }
    }
}
