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

            sb.AppendLine("DISCOVERED SKILLS");

            if (set.Commands.Count > 0)
            {
                var current = _loadout != null ? EquipmentTags.CurrentTags(_loadout) : new HashSet<string>();
                sb.AppendLine("Commands — press the key:");
                int i = 0;
                foreach (var c in set.Commands)
                {
                    string key = AbilitySlots.SlotLabel(i) ?? "unslotted";
                    var required = CommandGate.RequiredEquipment(c);
                    string status = required.Count > 0 && !CommandGate.Invocable(c, current)
                        ? $"  [LOCKED — needs {string.Join("/", required)}]"
                        : "";
                    sb.AppendLine($"  [{key}] {c.Name}{status}");
                    i++;
                }
            }
            if (set.Passives.Count > 0)
            {
                sb.AppendLine("\nPassives — always on:");
                foreach (var p in set.Passives)
                    sb.AppendLine($"  {p.Name}");
            }
            if (set.Weapons.Count > 0)
            {
                sb.AppendLine("\nWeapons — equip in city, fire to cast:");
                foreach (var w in set.Weapons)
                    sb.AppendLine($"  {w.Name}");
            }

            // Top-right, clear of the contract HUD / focus / gold on the left.
            GUI.Label(new Rect(Screen.width - 380f, 20f, 360f, 460f), sb.ToString());
        }

        private static string WeaponName(EquipmentSlot slot)
        {
            var data = slot != null && slot.Current != null ? slot.Current.Data : null;
            return data != null ? data.DisplayName : "(empty)";
        }
    }
}
