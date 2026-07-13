using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Discovered COMMANDS are cast from ability slots (hotkeys), not input combos. The combat
    /// inputs are all reused for normal fighting, so combo invocation misfired mid-fight and did
    /// not scale as commands accumulated. Each slot binds a discovered command to a key; pressing
    /// it casts the command, subject to the equipment gate (ADR 0011). Slots auto-fill from the
    /// discovered commands in discovery order (a reassignment UI is a follow-up).
    /// Destiny/Overwatch-style dedicated abilities — and since ADR 0012 removed the dodge button,
    /// a dash IS one of these: mobility is discovered, not issued.
    ///
    /// R and F are deliberately NOT used — they are the conventional reload / interact keys.
    ///
    /// Uses the low-level Keyboard (the project is on the new Input System, no legacy Input);
    /// rebinding through the action asset is a follow-up. Auto-provisioned by SkillCaster.
    /// </summary>
    public sealed class AbilitySlots : MonoBehaviour
    {
        public static readonly Key[] SlotKeys = { Key.Q, Key.E, Key.LeftShift, Key.C };
        public const int SlotCount = 4; // = SlotKeys.Length

        /// <summary>The hotkey label for a slot index, or null if out of range.</summary>
        public static string SlotLabel(int index)
            => index >= 0 && index < SlotKeys.Length ? Label(SlotKeys[index]) : null;

        private static string Label(Key key) => key == Key.LeftShift ? "Shift" : key.ToString();

        private SkillCaster _caster;
        private Loadout _loadout;

        private void Awake() => _caster = GetComponent<SkillCaster>() ?? FindAnyObjectByType<SkillCaster>();

        private void Update()
        {
            // A keyboard-focused panel is up (character creation's name field, the city board,
            // ...) — this polls Keyboard.current directly (not an input action), so it must honour
            // the shared UiFocus gate itself rather than relying on the "Player" action map alone.
            if (UiFocus.IsFocused) return;

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
            _caster?.ExecuteSkill(command.Skill, command.EffectiveGraph); // graph-driven (ADR 0007)
        }

        private HashSet<string> CurrentEquipment()
        {
            if (_loadout == null) _loadout = FindAnyObjectByType<Loadout>();
            return EquipmentTags.CurrentTags(_loadout);
        }
    }
}
