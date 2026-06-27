using UnityEngine;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Equipment;

namespace ProjectAscension.Game
{
    /// <summary>
    /// In the Frontier, equips the loadout the player chose in the City. When played
    /// directly (no GameSession), falls back to a serialized config. An authored weapon's
    /// stats come from the DB definition (fetched at startup) when available, so balance
    /// edits apply with no client rebuild; offline it uses the authored asset as-is.
    /// </summary>
    public sealed class LoadoutApplier : MonoBehaviour
    {
        [SerializeField] private LoadoutConfig fallback;

        private void Start()
        {
            var loadout = FindAnyObjectByType<Loadout>();
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

            loadout.Equip(WithServerStats(left), WithServerStats(right));
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
                def.spreadMin, def.spreadMax, def.spreadPerShot, def.spreadRecovery);
        }
    }
}
