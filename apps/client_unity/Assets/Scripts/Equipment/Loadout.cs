using UnityEngine;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// The player's two equip slots. Equips the pre-chosen LoadoutConfig (selected
    /// ahead of time from the inventory / City — not switched in the field) onto the
    /// hand anchors when the player spawns.
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

        private void Start()
        {
            _left = new EquipmentSlot(SlotType.Left, leftHand);
            _right = new EquipmentSlot(SlotType.Right, rightHand);

            if (config == null)
            {
                Debug.LogWarning("[Loadout] No LoadoutConfig assigned.", this);
                return;
            }

            if (config.Left != null) _left.Equip(WeaponFactory.Create(config.Left));
            if (config.Right != null) _right.Equip(WeaponFactory.Create(config.Right));
        }
    }
}
