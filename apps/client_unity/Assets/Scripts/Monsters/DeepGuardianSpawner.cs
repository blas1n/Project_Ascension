using UnityEngine;

namespace ProjectAscension.Monsters
{
    /// <summary>
    /// Places the guardian in the deep arena. Deliberately not a spawner in the usual sense — it puts
    /// exactly ONE thing in exactly ONE place, because stage 8 is authored: the player walks into the
    /// deep, meets it, and dies. A random roamer could be avoided or missed; a set-piece cannot.
    ///
    /// "사망은 연출된 경험이다" — the death is the point, and the point is not cruelty. It is the first
    /// honest thing the world says: you are not enough on your own, and you don't have to be
    /// (which is what 위임 and 발주 are for).
    /// </summary>
    public sealed class DeepGuardianSpawner : MonoBehaviour
    {
        private void Start()
        {
            var guardian = MonsterFactory.Create(MonsterType.Guardian, transform.position);
            guardian.name = "DeepGuardian";
        }
    }
}
