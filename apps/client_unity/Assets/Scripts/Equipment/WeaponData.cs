using UnityEngine;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// Authored definition of an equippable weapon/tool. Reuses the shared Domain
    /// enums so the client model matches the server. Combat-related fields are
    /// hooks for Phase 3 and unused for now.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Ascension/Weapon Data", fileName = "WeaponData")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Weapon";
        [SerializeField] private EquipmentType equipmentType = EquipmentType.Weapon;
        [SerializeField] private SlotType slotType = SlotType.Either;

        [Header("Combat (Phase 3 hooks — unused)")]
        [SerializeField] private float damage = 0f;
        [SerializeField] private float range = 0f;

        [Header("Discovery (later phase hook — unused)")]
        [SerializeField] private float discoveryWeight = 0f;

        public string DisplayName => displayName;
        public EquipmentType EquipmentType => equipmentType;
        public SlotType SlotType => slotType;
        public float Damage => damage;
        public float Range => range;
        public float DiscoveryWeight => discoveryWeight;
    }
}
