using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// A small always-on guide listing the player's discovered COMMANDS and the button combo
    /// that invokes each (the rule engine assigns every command an "incantation"), plus active
    /// passives and discovered weapons. Without it the player has no way to know how to trigger
    /// a discovered command — you press the shown sequence (e.g. Dodge > LMB) within the combo
    /// window. Immediate-mode GUI like DiscoveryNotification; auto-provisioned by SkillCaster,
    /// so no scene wiring. Uses no legacy Input API (project is on the new Input System).
    /// </summary>
    public sealed class SkillGuideHud : MonoBehaviour
    {
        private Loadout _loadout;

        private void OnGUI()
        {
            var set = GameSession.Instance?.DiscoveredSkills;
            if (set == null) return;

            var sb = new StringBuilder();

            // Which weapon each click fires — LMB = right hand, RMB = left hand (PlayerCombat).
            // Named explicitly because a bare "LMB" in a combo doesn't say which weapon it is,
            // and the two hands can be swapped.
            if (_loadout == null) _loadout = FindAnyObjectByType<Loadout>();
            if (_loadout != null)
            {
                sb.AppendLine("IN HAND:");
                sb.AppendLine($"  LMB → {WeaponName(_loadout.RightSlot)}");
                sb.AppendLine($"  RMB → {WeaponName(_loadout.LeftSlot)}");
                sb.AppendLine();
            }

            // Only what's USABLE right now — the four ability hotkeys and active passives. The
            // full list (which grows large) lives in the City, so this HUD never overflows.
            var current = _loadout != null ? EquipmentTags.CurrentTags(_loadout) : new HashSet<string>();
            var session = GameSession.Instance;

            sb.AppendLine("ABILITIES (hotkeys):");
            var slots = session != null ? session.CommandSlots : null;
            for (int i = 0; i < AbilitySlots.SlotKeys.Length; i++)
            {
                var cmd = slots != null && i < slots.Length ? slots[i] : null;
                if (cmd == null) { sb.AppendLine($"  [{AbilitySlots.SlotLabel(i)}] —"); continue; }
                string lockTxt = !CommandGate.Invocable(cmd, current)
                    ? $"  [LOCKED: {string.Join("/", CommandGate.RequiredEquipment(cmd))}]"
                    : "";
                sb.AppendLine($"  [{AbilitySlots.SlotLabel(i)}] {cmd.Name} — {SkillSummary.Describe(cmd.Skill)}{lockTxt}");
            }

            if (set.Passives.Count > 0)
            {
                sb.AppendLine("\nPassives (always on):");
                foreach (var p in set.Passives)
                    sb.AppendLine($"  {p.Name} — {SkillSummary.DescribePassive(p.Skill)}");
            }

            sb.AppendLine($"\nWeapons: {set.Weapons.Count} discovered (equip in city)");
            if (set.Commands.Count > AbilitySlots.SlotKeys.Length)
                sb.AppendLine($"Commands: {set.Commands.Count} discovered ({AbilitySlots.SlotKeys.Length} slots — assign in city)");

            // Top-right, clear of the contract HUD / focus / gold on the left.
            GUI.Label(new Rect(Screen.width - 380f, 20f, 360f, 300f), sb.ToString());
        }

        private static string WeaponName(EquipmentSlot slot)
        {
            var data = slot != null && slot.Current != null ? slot.Current.Data : null;
            return data != null ? data.DisplayName : "(empty)";
        }
    }
}
