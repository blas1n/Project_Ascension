using UnityEngine;

namespace ProjectAscension.Combat
{
    /// <summary>Where an attack comes from and where it is aimed.</summary>
    public readonly struct AttackContext
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly GameObject Attacker;

        public AttackContext(Vector3 origin, Vector3 direction, GameObject attacker)
        {
            Origin = origin;
            Direction = direction;
            Attacker = attacker;
        }
    }
}
