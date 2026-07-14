using System.Text;
using UnityEngine;
using ProjectAscension.Equipment;

namespace ProjectAscension.Game
{
    /// <summary>
    /// A small always-on guide listing what's in the player's hands and active passives, plus
    /// discovered-weapon/command counts. The ability hotkeys themselves (which command sits on
    /// which key, its cooldown) are the <see cref="AbilityBarHud"/>'s job now — an Overwatch-style
    /// box row is far more readable mid-fight than a text line, and duplicating both would drift.
    /// Immediate-mode GUI like DiscoveryNotification; auto-provisioned by SkillCaster, so no scene
    /// wiring. Uses no legacy Input API (project is on the new Input System).
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

            if (set.Passives.Count > 0)
            {
                sb.AppendLine("\nPassives (always on):");
                foreach (var p in set.Passives)
                    sb.AppendLine($"  {p.Name} — {SkillSummary.DescribePassive(p)}");
            }

            sb.AppendLine($"\nWeapons: {set.Weapons.Count} discovered (equip in city)");
            if (set.Commands.Count > AbilitySlots.SlotKeys.Length)
                sb.AppendLine($"Commands: {set.Commands.Count} discovered ({AbilitySlots.SlotKeys.Length} slots — assign in city)");

            // Top-right, clear of the contract HUD / gold on the left.
            GUI.Label(new Rect(Screen.width - 380f, 20f, 360f, 300f), sb.ToString());
        }

        private static string WeaponName(EquipmentSlot slot)
        {
            var data = slot != null && slot.Current != null ? slot.Current.Data : null;
            return data != null ? data.DisplayName : "(empty)";
        }
    }
}
