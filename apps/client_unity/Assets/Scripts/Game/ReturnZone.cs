using UnityEngine;
using ProjectAscension.World;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The pad that takes the player back to the City — on interact, not on touch. The old
    /// PlayerTriggerVolume version fired the instant you stepped on it, so simply crossing the
    /// frontier could yank you home without your say-so; press [F] instead. Not a
    /// PlayerTriggerVolume — an interactable doesn't need one (it self-registers, the sensor on the
    /// player finds it).
    /// </summary>
    public sealed class ReturnZone : MonoBehaviour
    {
        private const float Reach = 2.5f; // must be standing on/near the pad, not merely nearby

        private Interactable _interactable;

        private void Awake()
        {
            _interactable = gameObject.AddComponent<Interactable>();
            _interactable.Label = "Return to the city";
            _interactable.Reach = Reach;
            _interactable.Interacted += OnInteracted;
        }

        private void OnDestroy()
        {
            if (_interactable != null) _interactable.Interacted -= OnInteracted;
        }

        private void OnInteracted() => GameScenes.LoadCity();
    }
}
