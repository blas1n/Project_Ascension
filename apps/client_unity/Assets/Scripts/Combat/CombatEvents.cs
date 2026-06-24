using System;
using UnityEngine;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// Global combat notifications that cross assembly boundaries (e.g. contracts
    /// listening for kills without the monster code depending on the loop layer).
    /// </summary>
    public static class CombatEvents
    {
        /// <summary>Raised when a monster dies. Argument is the monster GameObject.</summary>
        public static event Action<GameObject> MonsterKilled;

        public static void RaiseMonsterKilled(GameObject monster) => MonsterKilled?.Invoke(monster);
    }
}
