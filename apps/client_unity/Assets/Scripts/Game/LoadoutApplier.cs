using UnityEngine;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Equipment;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Puts the chosen loadout in the player's hands — in the City and in the Frontier alike, and the
    /// INSTANT the choice changes, not at the next scene load. Picking a weapon at the equipment
    /// station is not filling in a form that takes effect when you leave town; it is picking up the
    /// weapon. You should see it in your hand while you are still standing at the rack — and when the
    /// art track puts a real model there, this is the seam that swaps it.
    ///
    /// When played directly (no GameSession), falls back to a serialized config. An authored weapon's
    /// stats come from the DB definition (fetched at startup) when available, so balance edits apply
    /// with no client rebuild; offline it uses the authored asset as-is.
    /// </summary>
    public sealed class LoadoutApplier : MonoBehaviour
    {
        [SerializeField] private LoadoutConfig fallback;

        private PlayerStateService _watched;

        private void Start() => Apply();

        private void OnDestroy()
        {
            if (_watched != null) _watched.LoadoutChanged -= Apply;
        }

        private void Apply()
        {
            var loadout = FindAnyObjectByType<Loadout>();
            if (loadout == null) return;

            WeaponData left, right;
            var session = GameSession.Instance;
            if (session != null && session.PlayerState != null)
            {
                Watch(session.PlayerState);
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

            loadout.Equip(WithServerStats(left), WithServerStats(right));
        }

        // Subscribe once — the state outlives the scene, so a re-subscribe per scene would stack.
        private void Watch(PlayerStateService state)
        {
            if (ReferenceEquals(_watched, state)) return;
            if (_watched != null) _watched.LoadoutChanged -= Apply;
            _watched = state;
            _watched.LoadoutChanged += Apply;
        }

        // Swap an authored weapon for a DB-driven build of the same weapon (matched by
        // display name). A discovered weapon, an offline session, or an unknown name all
        // fall through to the original asset.
        private static WeaponData WithServerStats(WeaponData authored)
        {
            if (authored == null || authored.DiscoveredSkill != null) return authored;
            var def = GameSession.Instance?.WeaponDefinition(authored.DisplayName);
            if (def == null) return authored;

            var equipmentType = System.Enum.TryParse<EquipmentType>(def.equipmentType, out var et) ? et : authored.EquipmentType;
            var slotType = System.Enum.TryParse<SlotType>(def.slotType, out var st) ? st : authored.SlotType;
            return WeaponData.CreateFromDefinition(
                def.displayName, equipmentType, slotType,
                def.damage, def.range, def.projectileSpeed, def.projectileGravity, def.cooldown,
                def.chargeTime, def.maxChargeMultiplier,
                def.spreadMin, def.spreadMax, def.spreadPerShot, def.spreadRecovery,
                def.magazineSize, def.reloadTime);
        }
    }
}
