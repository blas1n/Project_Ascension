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
    /// it casts the command, subject to the equipment gate (ADR 0011) and its own COOLDOWN
    /// (replaces the removed Focus resource — project-owner decision: per-skill cooldown,
    /// Overwatch-style; see SkillCooldown/SkillCooldownRules in GameSimulation). Nothing
    /// auto-binds a slot — a fresh discovery enters the journal only, and the player assigns it
    /// at the Equipment Station's hotkey picker (EquipmentStationPanel), which lists only the
    /// commands assignable with the current loadout (GameSimulation.Combat.AssignableCommands).
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

        // Per-slot cooldown state (pure DECISION in SkillCooldownRules; this only tracks it) —
        // a command "lives on exactly one key" (GameSession.AssignCommandSlot), so cooldown is
        // tracked per SLOT, not per discovered skill identity.
        private readonly float[] _nextReady = new float[SlotCount];
        private readonly float[] _cooldownDuration = new float[SlotCount];

        /// <summary>Seconds left before the slot's command may be cast again (0 = ready).</summary>
        public float CooldownRemaining(int index)
            => index >= 0 && index < SlotCount ? Mathf.Max(0f, _nextReady[index] - Time.time) : 0f;

        /// <summary>How far through its cooldown the slot is (1 = just cast, 0 = ready) — drives
        /// the HUD's radial/vertical sweep.</summary>
        public float CooldownFraction(int index)
        {
            if (index < 0 || index >= SlotCount) return 0f;
            float duration = _cooldownDuration[index];
            return duration > 0f ? Mathf.Clamp01(CooldownRemaining(index) / duration) : 0f;
        }

        /// <summary>The hotkey label for a slot index, or null if out of range. Sourced from
        /// PlayerInputHandler.KeyLabel (position-based — never the OS layout's display string, so
        /// this can't turn into a jamo on a Korean keyboard) rather than a hardcoded letter, so
        /// every hotkey prompt in the game agrees.</summary>
        public static string SlotLabel(int index)
            => index >= 0 && index < SlotKeys.Length ? PlayerInputHandler.KeyLabel(SlotKeys[index]) : null;

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

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var slots = session.CommandSlots;
            for (int i = 0; i < slots.Length && i < SlotKeys.Length; i++)
                if (slots[i] != null && keyboard[SlotKeys[i]].wasPressedThisFrame)
                    Cast(i, slots[i]);
        }

        private void Cast(int index, DiscoveredSkill command)
        {
            if (command == null) return;
            if (!CommandGate.Invocable(command, CurrentEquipment()))
            {
                Debug.Log($"[AbilitySlots] \"{command.Name}\" needs {string.Join("/", CommandGate.RequiredEquipment(command))} equipped.");
                return;
            }
            if (!SkillCooldownRules.CanCast(Time.time, _nextReady[index]))
            {
                Debug.Log($"[AbilitySlots] \"{command.Name}\" cooling down ({CooldownRemaining(index):F1}s left).");
                return;
            }

            _caster?.ExecuteSkill(command.Skill, command.EffectiveGraph); // graph-driven (ADR 0007)

            // Start this slot's cooldown — derived from the skill's effect graph, DB-driven rate
            // (SkillCooldown), never authored per skill.
            float duration = SkillCooldown.Of(command.EffectiveGraph, CombatTuningCatalog.Current);
            _cooldownDuration[index] = duration;
            _nextReady[index] = SkillCooldownRules.NextReady(Time.time, duration);
        }

        private HashSet<string> CurrentEquipment()
        {
            if (_loadout == null) _loadout = FindAnyObjectByType<Loadout>();
            return EquipmentTags.CurrentTags(_loadout);
        }
    }
}
