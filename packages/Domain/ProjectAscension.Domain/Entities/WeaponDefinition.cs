#nullable enable
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// An authored weapon's stats as server-managed, runtime-editable data — identity,
    /// combat, charge, and spread. Replaces hard-coded ScriptableObject numbers so a
    /// balance designer can retune a starter weapon (e.g. arrow drop) from the DB with
    /// no client rebuild. The client fetches these and builds its weapon instances.
    /// </summary>
    public class WeaponDefinition
    {
        public string Key { get; set; } = string.Empty; // stable id: "sword","bow","pistol","catalyst"
        public string DisplayName { get; set; } = string.Empty;
        public EquipmentType EquipmentType { get; set; }
        public SlotType SlotType { get; set; }

        // Combat.
        public float Damage { get; set; }
        public float Range { get; set; }
        public float ProjectileSpeed { get; set; } // 0 = hitscan/melee
        public float ProjectileGravity { get; set; } // >0 = drops/arcs (arrow)
        public float Cooldown { get; set; }

        // Charge (0 = instant).
        public float ChargeTime { get; set; }
        public float MaxChargeMultiplier { get; set; }

        // Spread / accuracy (firearms; 0 max = precise).
        public float SpreadMin { get; set; }
        public float SpreadMax { get; set; }
        public float SpreadPerShot { get; set; }
        public float SpreadRecovery { get; set; }

        // Magazine (firearms; 0 = no magazine, never reloads — the vulnerability beat of running
        // dry is the point, not an ammo economy: there is no reserve, only the magazine).
        public int MagazineSize { get; set; }
        public float ReloadTime { get; set; }
    }
}
