using UnityEngine;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// The player's two equip slots. Equips a pre-chosen pair onto the hand anchors.
    /// A serialized LoadoutConfig is used standalone; in the loop the selection is
    /// applied at runtime via <see cref="Equip"/>.
    /// </summary>
    public sealed class Loadout : MonoBehaviour
    {
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private LoadoutConfig config;

        private EquipmentSlot _left;
        private EquipmentSlot _right;

        public EquipmentSlot LeftSlot => _left;
        public EquipmentSlot RightSlot => _right;

        private void Awake()
        {
            _left = new EquipmentSlot(SlotType.Left, leftHand);
            _right = new EquipmentSlot(SlotType.Right, rightHand);
        }

        private void Start()
        {
            if (config != null)
                Equip(config.Left, config.Right);
        }

        /// <summary>Re-equip both slots from explicit weapon data (runtime selection).
        /// A two-handed weapon occupies both slots (the other hand stays empty).</summary>
        public void Equip(WeaponData left, WeaponData right)
        {
            if (left != null && left.IsTwoHand) { EquipTwoHand(left); return; }
            if (right != null && right.IsTwoHand) { EquipTwoHand(right); return; }
            if (left != null) _left.Equip(WeaponFactory.Create(left)); else _left.Unequip();
            if (right != null) _right.Equip(WeaponFactory.Create(right)); else _right.Unequip();
        }

        private void EquipTwoHand(WeaponData data)
        {
            _left.Unequip(); // both hands hold the one weapon; it fires on the primary input
            _right.Equip(WeaponFactory.Create(data));
        }

        /// <summary>Equip a single weapon into the left hand (e.g. a freshly discovered
        /// weapon), keeping the right hand as-is.</summary>
        public void EquipLeft(WeaponData weapon)
        {
            if (weapon != null) _left.Equip(WeaponFactory.Create(weapon));
        }
    }
}
