using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Game
{
    /// <summary>Frontier toast shown when a discovery finishes composing — with the SERVER-composed
    /// name (GameplayEvents.SkillDiscovered). The client no longer names discoveries locally; the
    /// server is the sole authority (ADR 0002/0004), so the toast and the city skill list always
    /// agree.</summary>
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

        private void OnDiscovered(string name)
        {
            _toasts.Add(new Toast { Text = $"Discovery! {name}", Until = Time.time + 4.5f });
        }

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
