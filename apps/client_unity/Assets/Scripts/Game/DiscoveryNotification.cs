using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.GameSimulation.Discovery;

namespace ProjectAscension.Game
{
    /// <summary>Frontier toast shown when a discovery is unlocked.</summary>
    public sealed class DiscoveryNotification : MonoBehaviour
    {
        private struct Toast
        {
            public string Text;
            public float Until;
        }

        private readonly List<Toast> _toasts = new();

        private void OnEnable()
        {
            if (GameSession.Instance != null)
                GameSession.Instance.Discovery.Unlocked += OnUnlocked;
        }

        private void OnDisable()
        {
            if (GameSession.Instance != null)
                GameSession.Instance.Discovery.Unlocked -= OnUnlocked;
        }

        private void OnUnlocked(DiscoveryCandidate candidate)
        {
            _toasts.Add(new Toast { Text = $"Discovery! {candidate.Title}", Until = Time.time + 4.5f });
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
