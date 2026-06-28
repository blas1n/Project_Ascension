using System.Collections.Generic;

namespace ProjectAscension.Combat
{
    /// <summary>A monster type's combat stats (DB-driven). Lives in Combat so the Game
    /// layer (which fetches them) and the Monsters layer (which builds from them) can
    /// both see it without referencing each other.</summary>
    public sealed class MonsterStats
    {
        public readonly float MaxHealth;
        public readonly float MoveSpeed;
        public readonly float AggroRange;
        public readonly float AttackRange;
        public readonly float AttackCooldown;
        public readonly float Damage;
        public readonly float ProjectileSpeed; // 0 = melee
        public readonly float Scale;
        public readonly string DropItemKey;
        public readonly int DropAmount;

        public MonsterStats(float maxHealth, float moveSpeed, float aggroRange, float attackRange,
            float attackCooldown, float damage, float projectileSpeed, float scale,
            string dropItemKey = "", int dropAmount = 0)
        {
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            AggroRange = aggroRange;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            Damage = damage;
            ProjectileSpeed = projectileSpeed;
            Scale = scale;
            DropItemKey = dropItemKey ?? "";
            DropAmount = dropAmount;
        }
    }

    /// <summary>Process-wide cache of DB-driven monster stats by key ("melee"/"ranged"/
    /// "elite"). The Game layer fills it from the server at startup; the monster factory
    /// reads it, falling back to its built-in defaults when a key is absent (offline).</summary>
    public static class MonsterStatsCatalog
    {
        private static readonly Dictionary<string, MonsterStats> _byKey = new();

        public static void Set(string key, MonsterStats stats)
        {
            if (!string.IsNullOrEmpty(key)) _byKey[key] = stats;
        }

        public static bool TryGet(string key, out MonsterStats stats)
            => _byKey.TryGetValue(key ?? string.Empty, out stats);
    }
}
