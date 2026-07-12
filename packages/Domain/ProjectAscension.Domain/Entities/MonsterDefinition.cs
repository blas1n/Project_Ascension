#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// A monster type's combat stats as server-managed, runtime-editable data. Moves the
    /// numbers out of the client factory so balance — and, later, AI/dynamic systems
    /// (World Will, adaptive difficulty) — can read and reshape them from the DB. The
    /// client fetches these and builds its monsters from them.
    /// </summary>
    public class MonsterDefinition
    {
        public string Key { get; set; } = string.Empty; // stable id: "melee","ranged","elite"
        public float MaxHealth { get; set; }
        public float MoveSpeed { get; set; }
        public float AggroRange { get; set; }
        public float AttackRange { get; set; }
        public float AttackCooldown { get; set; }
        public float AttackWindup { get; set; } // telegraph seconds before a strike lands; 0 = instant
        public float Damage { get; set; }
        public float ProjectileSpeed { get; set; } // 0 = melee
        public float Scale { get; set; } // visual/body scale (elite is larger)

        // The resource this monster drops on death (the itemization foundation — drops
        // feed the shop, contract collection, and settlement supply). Empty = no drop.
        public string DropItemKey { get; set; } = string.Empty;
        public int DropAmount { get; set; }
    }
}
