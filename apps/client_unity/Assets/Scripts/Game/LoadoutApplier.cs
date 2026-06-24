using UnityEngine;
using ProjectAscension.Equipment;

namespace ProjectAscension.Game
{
    /// <summary>
    /// In the Frontier, equips the loadout the player chose in the City. When played
    /// directly (no GameSession), falls back to a serialized config.
    /// </summary>
    public sealed class LoadoutApplier : MonoBehaviour
    {
        [SerializeField] private LoadoutConfig fallback;

        private void Start()
        {
            var loadout = FindFirstObjectByType<Loadout>();
            if (loadout == null) return;

            WeaponData left, right;
            var session = GameSession.Instance;
            if (session != null)
            {
                left = session.PlayerState.SelectedLeft;
                right = session.PlayerState.SelectedRight;
            }
            else if (fallback != null)
            {
                left = fallback.Left;
                right = fallback.Right;
            }
            else
            {
                return;
            }

            loadout.Equip(left, right);
        }
    }
}
