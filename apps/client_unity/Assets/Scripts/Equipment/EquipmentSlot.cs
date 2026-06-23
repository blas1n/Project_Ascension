using UnityEngine;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// A single equip position (left or right hand). Holds at most one equippable
    /// and attaches it to its hand anchor.
    /// </summary>
    public sealed class EquipmentSlot
    {
        public SlotType Side { get; }
        public Transform Anchor { get; }
        public IEquippable Current { get; private set; }

        public EquipmentSlot(SlotType side, Transform anchor)
        {
            Side = side;
            Anchor = anchor;
        }

        public void Equip(IEquippable equippable)
        {
            Unequip();
            Current = equippable;
            Current?.OnEquip(Anchor);
        }

        public void Unequip()
        {
            Current?.OnUnequip();
            Current = null;
        }
    }
}
