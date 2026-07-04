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

        /// <summary>The hotkey label for the command at a given slot index, or null if it has
        /// no slot (more commands than keys). Deterministic (discovery order) so the City and
        /// Frontier guides agree without this component present.</summary>
        public static string SlotLabel(int index)
            => index >= 0 && index < SlotKeys.Length ? SlotKeys[index].ToString() : null;

        private SkillCaster _caster;
        private Loadout _loadout;
        private readonly List<DiscoveredSkill> _slots = new();

        private void Awake() => _caster = GetComponent<SkillCaster>() ?? FindAnyObjectByType<SkillCaster>();

        private void Update()
        {
            SyncSlots();
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            for (int i = 0; i < _slots.Count && i < SlotKeys.Length; i++)
                if (keyboard[SlotKeys[i]].wasPressedThisFrame)
                    Cast(_slots[i]);
        }

        // Fill slots in discovery order, up to the number of keys. (Reassignment UI is a follow-up.)
        private void SyncSlots()
        {
            var set = GameSession.Instance != null ? GameSession.Instance.DiscoveredSkills : null;
            if (set == null) return;
            foreach (var command in set.Commands)
            {
                if (_slots.Count >= SlotKeys.Length) break;
                if (!_slots.Contains(command)) _slots.Add(command);
            }
        }

        private void Cast(DiscoveredSkill command)
        {
            if (command == null) return;
            if (!CommandGate.Invocable(command, CurrentEquipment()))
            {
                Debug.Log($"[AbilitySlots] \"{command.Name}\" needs {string.Join("/", CommandGate.RequiredEquipment(command))} equipped.");
                return;
            }
            _caster?.ExecuteSkill(command.Skill);
        }

        private HashSet<string> CurrentEquipment()
        {
            if (_loadout == null) _loadout = FindAnyObjectByType<Loadout>();
            return EquipmentTags.CurrentTags(_loadout);
        }
    }
}
