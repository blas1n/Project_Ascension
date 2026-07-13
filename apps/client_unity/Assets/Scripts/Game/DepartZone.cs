using UnityEngine;
using ProjectAscension.World;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The gate that sends the player to the frontier — on interact, not on touch, mirroring
    /// <see cref="ReturnZone"/>. Departure used to be a button inside the equipment station's own
    /// open panel (CLAUDE.md Phase 1's "expedition loop" needs a real place to leave FROM, not a menu
    /// item) — that button unloaded the City scene from inside an OnGUI call while the panel still
    /// held the UiFocus gate, which is exactly the class of bug CityStationPanel.OnDestroy's defensive
    /// Pop() exists for. A standalone pad sidesteps it entirely: nothing here ever opens a panel, so
    /// there is no focus to still be holding when the scene unloads.
    /// </summary>
    public sealed class DepartZone : MonoBehaviour
    {
        private Interactable _interactable;

        private void Awake()
        {
            _interactable = gameObject.AddComponent<Interactable>();
            _interactable.Label = "Depart to the frontier";
            _interactable.Reach = CityBlockout.GateReach;
            _interactable.Interacted += OnInteracted;
        }

        private void OnDestroy()
        {
            if (_interactable != null) _interactable.Interacted -= OnInteracted;
        }

        private void OnInteracted()
        {
            GameSession.Instance?.Save(); // persist progress before leaving, same as the old button did
            GameScenes.LoadFrontier();
        }
    }
}
