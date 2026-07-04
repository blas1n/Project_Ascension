using System.Collections.Generic;
using System.Text;
using UnityEngine;
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
        private void OnGUI()
        {
            var set = GameSession.Instance?.DiscoveredSkills;
            if (set == null || (set.Commands.Count == 0 && set.Passives.Count == 0 && set.Weapons.Count == 0))
                return;

            var sb = new StringBuilder();
            sb.AppendLine("DISCOVERED SKILLS");

            if (set.Commands.Count > 0)
            {
                sb.AppendLine("\nCommands — press the combo:");
                foreach (var c in set.Commands)
                    sb.AppendLine($"  {c.Name}:  {ComboText(c.Combo)}");
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

        private static string ComboText(IReadOnlyList<InputToken> combo)
        {
            if (combo == null || combo.Count == 0) return "(no combo)";
            var parts = new string[combo.Count];
            for (int i = 0; i < combo.Count; i++) parts[i] = Key(combo[i]);
            return string.Join(" > ", parts);
        }

        private static string Key(InputToken t) => t switch
        {
            InputToken.Jump => "Jump",
            InputToken.Dodge => "Dodge",
            InputToken.LeftClick => "LMB",
            InputToken.RightClick => "RMB",
            _ => t.ToString(),
        };
    }
}
