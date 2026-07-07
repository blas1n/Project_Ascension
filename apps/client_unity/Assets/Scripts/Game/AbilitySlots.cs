using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Discovered COMMANDS are cast from ability slots (hotkeys), not input combos. With only
    /// four combat inputs (jump/dodge/LMB/RMB) — all reused for normal fighting — combo
    /// invocation misfired during combat and didn't scale as commands accumulated. Each slot
    /// binds a discovered command to a key (Q/E/R/F); pressing it casts the command, subject to
    /// the equipment gate (ADR 0005). Slots auto-fill from the discovered commands in discovery
    /// order (a reassignment UI is a follow-up). Destiny-style dedicated abilities.
    ///
    /// Uses the low-level Keyboard (the project is on the new Input System, no legacy Input),
    /// so no .inputactions asset edit is needed. Auto-provisioned by SkillCaster.
    /// </summary>
    public sealed class AbilitySlots : MonoBehaviour
    {
        public static readonly Key[] SlotKeys = { Key.Q, Key.E, Key.R, Key.F };
        public const int SlotCount = 4; // = SlotKeys.Length

        /// <summary>The hotkey label for a slot index (Q/E/R/F), or null if out of range.</summary>
        public static string SlotLabel(int index)
            => index >= 0 && index < SlotKeys.Length ? SlotKeys[index].ToString() : null;

        private SkillCaster _caster;
        private Loadout _loadout;

        private void Awake() => _caster = GetComponent<SkillCaster>() ?? FindAnyObjectByType<SkillCaster>();

        private void Update()
        {
            var session = GameSession.Instance;
            if (session == null) return;
            session.EnsureDefaultCommandSlots(); // seed the bar with the first commands until the player customises it

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var slots = session.CommandSlots;
            for (int i = 0; i < slots.Length && i < SlotKeys.Length; i++)
                if (slots[i] != null && keyboard[SlotKeys[i]].wasPressedThisFrame)
                    Cast(slots[i]);
        }

        private void Cast(DiscoveredSkill command)
        {
            if (command == null) return;
            if (!CommandGate.Invocable(command, CurrentEquipment()))
            {
                Debug.Log($"[AbilitySlots] \"{command.Name}\" needs {string.Join("/", CommandGate.RequiredEquipment(command))} equipped.");
                return;
            }
            _caster?.ExecuteSkill(command.Skill, command.Graph); // graph-driven when composed (ADR 0007)
        }

        private HashSet<string> CurrentEquipment()
        {
            if (_loadout == null) _loadout = FindAnyObjectByType<Loadout>();
            return EquipmentTags.CurrentTags(_loadout);
        }
    }
}
