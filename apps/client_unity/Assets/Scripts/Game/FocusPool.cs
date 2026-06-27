using UnityEngine;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The player's focus — the resource skills spend (combat-framework.md 집중력). It
    /// regenerates over time; the deterministic transitions live in FocusRules. A simple
    /// on-screen readout for now (HUD art later).
    /// </summary>
    public sealed class FocusPool : MonoBehaviour
    {
        [SerializeField] private float maxFocus = 100f;
        [SerializeField] private float regenPerSecond = 15f;

        private Focus _focus;

        public float Current => _focus.Current;
        public float Max => _focus.Max;

        private void Awake() => _focus = Focus.Full(maxFocus);

        private void Update() => _focus = FocusRules.Regenerate(_focus, regenPerSecond * Time.deltaTime);

        /// <summary>Spend the cost if affordable; returns false (and spends nothing) otherwise.</summary>
        public bool TrySpend(float cost)
        {
            if (!FocusRules.TrySpend(_focus, cost, out var next)) return false;
            _focus = next;
            return true;
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(20f, 50f, 300f, 24f), $"Focus {_focus.Current:F0}/{_focus.Max:F0}");
        }
    }
}
