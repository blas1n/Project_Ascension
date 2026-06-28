using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Monsters
{
    /// <summary>Spawns a placeholder monster (capsule) of the given type. Stats are
    /// DB-driven (fetched into <see cref="MonsterStatsCatalog"/> at startup); the built-in
    /// defaults below are the offline fallback. Only the placeholder color is cosmetic.</summary>
    public static class MonsterFactory
    {
        public static MonsterBase Create(MonsterType type, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"{type}Monster";
            go.transform.position = position;

            var stats = StatsFor(type);
            var hr = go.AddComponent<HitReceiver>();
            hr.SetMaxHealth(stats.MaxHealth);
            go.transform.localScale = Vector3.one * stats.Scale;

            MonsterBase monster = type switch
            {
                MonsterType.Ranged => go.AddComponent<RangedMonster>(),
                MonsterType.Elite => go.AddComponent<EliteMonster>(),
                _ => go.AddComponent<MeleeMonster>(),
            };
            monster.Configure(stats.MoveSpeed, stats.AggroRange, stats.AttackRange,
                stats.AttackCooldown, stats.Damage, stats.ProjectileSpeed);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = ColorFor(type);

            monster.DiscoveryTag = "monster:" + type.ToString().ToLowerInvariant(); // discovery catalyst
            monster.DropItemKey = stats.DropItemKey;
            monster.DropAmount = stats.DropAmount;
            return monster;
        }

        // DB stats when fetched, else the built-in defaults (offline fallback).
        private static MonsterStats StatsFor(MonsterType type)
            => MonsterStatsCatalog.TryGet(type.ToString().ToLowerInvariant(), out var s) ? s : Default(type);

        private static MonsterStats Default(MonsterType type) => type switch
        {
            //                        hp     spd   aggro  atkRng  cd    dmg  projSpd scale  drop
            MonsterType.Ranged => new(25f, 2f, 30f, 14f, 1.5f, 6f, 18f, 1f, "feather", 2),
            MonsterType.Elite => new(120f, 2.5f, 35f, 18f, 1.2f, 14f, 24f, 1.6f, "core", 1),
            _ => new(40f, 3.5f, 25f, 2f, 1f, 8f, 0f, 1f, "hide", 2), // Melee
        };

        private static Color ColorFor(MonsterType type) => type switch
        {
            MonsterType.Ranged => new Color(1f, 0.5f, 0.2f),
            MonsterType.Elite => new Color(0.7f, 0.2f, 0.9f),
            _ => new Color(0.85f, 0.2f, 0.2f),
        };
    }
}
