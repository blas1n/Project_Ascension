using UnityEngine;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// Something that can occupy a left/right equipment slot. Weapons and (later)
    /// magic tools implement this. Combat behaviour is added in Phase 3.
    /// </summary>
    public interface IEquippable
    {
        WeaponData Data { get; }

        /// <summary>Attach to a hand anchor and become active.</summary>
        void OnEquip(Transform handAnchor);

        /// <summary>Detach / hide when removed from the slot.</summary>
        void OnUnequip();
    }
}
