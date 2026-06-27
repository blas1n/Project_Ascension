using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Monsters
{
    /// <summary>Spawns a placeholder monster (capsule) of the given type, tuned per tier.</summary>
    public static class MonsterFactory
    {
        public static MonsterBase Create(MonsterType type, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"{type}Monster";
            go.transform.position = position;

            var hr = go.AddComponent<HitReceiver>();
            MonsterBase monster;
            Color color;

            switch (type)
            {
                case MonsterType.Ranged:
                    hr.SetMaxHealth(25f);
                    var ranged = go.AddComponent<RangedMonster>();
                    ranged.Configure(moveSpeed: 2f, aggroRange: 30f, attackRange: 14f, attackCooldown: 1.5f, damage: 6f, projectileSpeed: 18f);
                    monster = ranged;
                    color = new Color(1f, 0.5f, 0.2f);
                    break;

                case MonsterType.Elite:
                    hr.SetMaxHealth(120f);
                    go.transform.localScale = Vector3.one * 1.6f;
                    var elite = go.AddComponent<EliteMonster>();
                    elite.Configure(moveSpeed: 2.5f, aggroRange: 35f, attackRange: 18f, attackCooldown: 1.2f, damage: 14f, projectileSpeed: 24f);
                    monster = elite;
                    color = new Color(0.7f, 0.2f, 0.9f);
                    break;

                default: // Melee
                    hr.SetMaxHealth(40f);
                    var melee = go.AddComponent<MeleeMonster>();
                    melee.Configure(moveSpeed: 3.5f, aggroRange: 25f, attackRange: 2f, attackCooldown: 1f, damage: 8f, projectileSpeed: 0f);
                    monster = melee;
                    color = new Color(0.85f, 0.2f, 0.2f);
                    break;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;

            monster.DiscoveryTag = "monster:" + type.ToString().ToLowerInvariant(); // discovery catalyst
            return monster;
        }
    }
}
