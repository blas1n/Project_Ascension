using System.Collections.Generic;
using UnityEngine;
using VContainer;
using ProjectAscension.GameSimulation.World;
using ProjectAscension.World;

namespace ProjectAscension.Player
{
    /// <summary>
    /// Lives on the player. Every frame it measures the XZ distance from the player to every
    /// registered <see cref="Interactable"/> and hands the list to the pure
    /// <see cref="InteractionRules.Best"/> — which one (if any) is nearest AND within its own reach is
    /// a decision, so it does not get made here (ADR: Unity is a shell). On the Interact input it
    /// fires <see cref="Current"/>'s Interact() and nothing else; it does not know or care what that
    /// does (open a panel, load a scene, ...).
    /// </summary>
    public sealed class InteractionSensor : MonoBehaviour
    {
        // Reused every frame instead of allocating — this runs once per player per frame for the
        // whole slice, but there is no reason to churn the GC for it.
        private readonly List<Interactable> _inRange = new();
        private readonly List<InteractCandidate> _candidates = new();

        private PlayerInputHandler _input;

        /// <summary>The interactable the prompt should show and Interact() would fire right now, or
        /// null if nothing qualifies. One player in the slice, so a static read surface matches the
        /// rest of the codebase (TutorialRunner.Instance, CityNpc.NearIssuer).</summary>
        public static Interactable Current { get; private set; }

        [Inject]
        public void Construct(PlayerInputHandler input) => _input = input;

        private void Start()
        {
            if (_input == null)
            {
                Debug.LogError("[InteractionSensor] PlayerInputHandler was not injected. Ensure a " +
                    "FrontierLifetimeScope with InputActions assigned exists in the scene.", this);
                enabled = false;
                return;
            }

            _input.InteractPressed += OnInteractPressed;
        }

        private void OnDestroy()
        {
            if (_input != null) _input.InteractPressed -= OnInteractPressed;
            if (Current != null) Current = null;
        }

        private void Update()
        {
            _inRange.Clear();
            _candidates.Clear();

            var all = Interactable.All;
            var pos = transform.position;
            for (int i = 0; i < all.Count; i++)
            {
                var candidate = all[i];
                // A disabled interactable (e.g. a hidden NPC) still lingers in All between OnDisable
                // and Destroy in some teardown orders — skip it rather than trust list membership alone.
                if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;

                var delta = candidate.transform.position - pos;
                delta.y = 0f;

                _inRange.Add(candidate);
                _candidates.Add(new InteractCandidate(_inRange.Count - 1, delta.magnitude, candidate.Reach));
            }

            int best = InteractionRules.Best(_candidates);
            Current = best >= 0 ? _inRange[best] : null;
        }

        private void OnInteractPressed() => Current?.Interact();
    }
}
