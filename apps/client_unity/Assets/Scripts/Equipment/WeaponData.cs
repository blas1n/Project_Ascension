using UnityEngine;
using ProjectAscension.Domain.Enums;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Equipment
{
    /// <summary>
    /// Authored definition of an equippable weapon/tool. Reuses the shared Domain
    /// enums so the client model matches the server. Combat-related fields are
    /// hooks for Phase 3 and unused for now. A discovered weapon is a runtime instance
    /// (<see cref="CreateDiscovered"/>) carrying its discovered <see cref="DiscoveredSkill"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Ascension/Weapon Data", fileName = "WeaponData")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Weapon";
        [SerializeField] private EquipmentType equipmentType = EquipmentType.Weapon;
        [SerializeField] private SlotType slotType = SlotType.Either;

        [Header("Combat")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float range = 50f;
        [SerializeField] private float projectileSpeed = 0f; // 0 = hitscan/melee
        [SerializeField] private float cooldown = 0.4f;

        [Header("Discovery (later phase hook — unused)")]
        [SerializeField] private float discoveryWeight = 0f;

        public string DisplayName => displayName;
        public EquipmentType EquipmentType => equipmentType;
        public SlotType SlotType => slotType;
        public float Damage => damage;
        public float Range => range;
        public float ProjectileSpeed => projectileSpeed;
        public float Cooldown => cooldown;
        public float DiscoveryWeight => discoveryWeight;

        /// <summary>Melee weapons strike in range; everything else is ranged.</summary>
        public bool IsMelee => equipmentType == EquipmentType.Weapon;

        // Runtime-only (a discovered weapon), not serialized.
        private Skill _discoveredSkill;
        private string _contextTag;

        /// <summary>The discovered skill this weapon casts (null for authored weapons).</summary>
        public Skill DiscoveredSkill => _discoveredSkill;

        /// <summary>A distinct equipment-context tag a discovered weapon contributes to
        /// the discovery context — so equipping it opens further discoveries (the loop).
        /// Null for authored weapons (which use their EquipmentType tag).</summary>
        public string ContextTag => _contextTag;

        /// <summary>Mint a discovered weapon at runtime from a composed skill — a new
        /// equippable that casts it (ADR 0005: a weapon is a new slot item).</summary>
        public static WeaponData CreateDiscovered(string name, Skill skill, string contextTag)
        {
            var data = CreateInstance<WeaponData>();
            data.displayName = string.IsNullOrEmpty(name) ? "Discovery" : name;
            data.equipmentType = EquipmentType.Catalyst; // a spell-casting weapon
            data.slotType = SlotType.Either;
            data.cooldown = 0.5f;
            data._discoveredSkill = skill;
            data._contextTag = contextTag;
            return data;
        }
    }
}
