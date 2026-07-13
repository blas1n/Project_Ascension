using UnityEngine;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Monsters;

namespace ProjectAscension.Monsters
{
    /// <summary>Spawns the three monster types around itself on scene start. WHERE each one lands is a
    /// deterministic sim fact (SpawnPlacement), not a client dice roll (ADR: Unity is a shell) — this
    /// component only supplies the seed and turns the sim's offsets into world positions.</summary>
    public sealed class MonsterSpawner : MonoBehaviour
    {
        [SerializeField] private int meleeCount = 3;
        [SerializeField] private int rangedCount = 2;
        [SerializeField] private int eliteCount = 1;
        [SerializeField] private float radius = 18f;
        [SerializeField] private float minDistance = 8f;

        // A seed this spawner can justify — authored per placement, not re-rolled — so the wave it
        // produces is reproducible (replay, or a future authoritative server placing the same wave).
        [SerializeField] private uint seed = 20260713u;

        private void Start()
        {
            Spawn(MonsterType.Melee, meleeCount);
            Spawn(MonsterType.Ranged, rangedCount);
            Spawn(MonsterType.Elite, eliteCount);
        }

        private void Spawn(MonsterType type, int count)
        {
            // Each monster type draws its own stream (salted off the base seed) so the ranged wave
            // doesn't land on top of the melee wave's first N points.
            uint typeSeed = DeterministicRng.Combine(seed, (uint)type);
            var offsets = SpawnPlacement.Ring(typeSeed, count, minDistance, radius);
            foreach (var (x, z) in offsets)
                MonsterFactory.Create(type, new Vector3(transform.position.x + x, 1f, transform.position.z + z));
        }
    }
}
