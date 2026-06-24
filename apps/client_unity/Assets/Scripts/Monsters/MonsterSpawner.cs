using UnityEngine;

namespace ProjectAscension.Monsters
{
    /// <summary>Spawns the three monster types around itself on scene start.</summary>
    public sealed class MonsterSpawner : MonoBehaviour
    {
        [SerializeField] private int meleeCount = 3;
        [SerializeField] private int rangedCount = 2;
        [SerializeField] private int eliteCount = 1;
        [SerializeField] private float radius = 18f;
        [SerializeField] private float minDistance = 8f;

        private void Start()
        {
            Spawn(MonsterType.Melee, meleeCount);
            Spawn(MonsterType.Ranged, rangedCount);
            Spawn(MonsterType.Elite, eliteCount);
        }

        private void Spawn(MonsterType type, int count)
        {
            for (int i = 0; i < count; i++)
                MonsterFactory.Create(type, RandomPoint());
        }

        private Vector3 RandomPoint()
        {
            var offset = Random.insideUnitCircle.normalized * Random.Range(minDistance, radius);
            return new Vector3(transform.position.x + offset.x, 1f, transform.position.z + offset.y);
        }
    }
}
