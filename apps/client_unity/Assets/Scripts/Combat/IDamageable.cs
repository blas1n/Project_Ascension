using UnityEngine;

namespace ProjectAscension.Combat
{
    /// <summary>Anything that can receive damage (player, monsters).</summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(float amount, GameObject source);
    }
}
