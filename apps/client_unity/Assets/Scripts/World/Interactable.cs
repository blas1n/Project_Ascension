using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscension.World
{
    /// <summary>
    /// A thing in the world the player can press [F] on — the contract board, the quartermaster, the
    /// return pad. This component only ADVERTISES itself (a label, its own reach) and reacts when
    /// told to; it never decides whether it is the one that should fire this frame. That decision is
    /// <c>InteractionRules.Best</c> (headless, testable — ADR: Unity is a shell), run by the sensor on
    /// the player against every entry in <see cref="All"/>.
    ///
    /// Self-registers on enable/disable into a flat static list instead of being discovered with
    /// FindObjectsOfType: the sensor reads this once per frame and a per-frame scene scan does not
    /// scale.
    /// </summary>
    public sealed class Interactable : MonoBehaviour
    {
        [SerializeField] private string label = "";

        /// <summary>How far away THIS interactable can be triggered from. Carried per-instance rather
        /// than as one global radius — a board should be readable from across the square; a lootable
        /// only when you are standing on it.</summary>
        [SerializeField] private float reach = 3f;

        /// <summary>Every enabled interactable in the scene. The sensor is the only reader; nothing
        /// else should mutate this list.</summary>
        public static readonly List<Interactable> All = new();

        /// <summary>What the [F] prompt should read, e.g. "Contract Board".</summary>
        public string Label
        {
            get => label;
            set => label = value;
        }

        /// <inheritdoc cref="reach"/>
        public float Reach
        {
            get => reach;
            set => reach = value;
        }

        /// <summary>Raised when the player presses Interact while this is the sensor's current target.
        /// Carries no payload — the listener already knows what it is (it owns this component).</summary>
        public event Action Interacted;

        /// <summary>Fire the interaction. Called by the sensor only; never call this to "check" whether
        /// interacting is possible — reach/selection already happened in InteractionRules.</summary>
        public void Interact() => Interacted?.Invoke();

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);
    }
}
