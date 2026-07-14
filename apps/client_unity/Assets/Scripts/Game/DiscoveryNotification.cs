using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>Frontier toast shown when a discovery finishes composing — with the SERVER-composed
    /// name (GameplayEvents.SkillDiscovered). The client no longer names discoveries locally; the
    /// server is the sole authority (ADR 0002/0004), so the toast and the city skill list always
    /// agree.
    ///
    /// A discovery is not an equip (see SkillCaster.OnSkillReady): a weapon lands in the inventory,
    /// a command in the journal, neither in the player's hands. So the toast says WHERE to go claim
    /// it — one short line, so the player isn't left hunting for what just happened.</summary>
    public sealed class DiscoveryNotification : MonoBehaviour
    {
        private struct Toast
        {
            public string Text;
            public float Until;
        }

        private readonly List<Toast> _toasts = new();

        private void OnEnable() => GameplayEvents.SkillDiscovered += OnDiscovered;
        private void OnDisable() => GameplayEvents.SkillDiscovered -= OnDiscovered;

        private void OnDiscovered(string name, ManifestationKind manifestation)
        {
            string text = $"Discovery! {name} — {Claim(manifestation)}";
            _toasts.Add(new Toast { Text = text, Until = Time.time + 4.5f });
        }

        private static string Claim(ManifestationKind manifestation) => manifestation switch
        {
            ManifestationKind.Weapon => "new weapon in your inventory, equip it at the Equipment Station",
            ManifestationKind.Passive => "passive, always active",
            _ => "new command, bind it to a hotkey at the Equipment Station",
        };

        private void OnGUI()
        {
            float y = 150f;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _toasts[i].Until)
                {
                    _toasts.RemoveAt(i);
                    continue;
                }
                GUI.Label(new Rect(20f, y, 420f, 24f), _toasts[i].Text);
                y += 24f;
            }
        }
    }
}
