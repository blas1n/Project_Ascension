using System;
using UnityEngine;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Combat
{
    /// <summary>
    /// Holds health and applies damage through the deterministic CombatResolver.
    /// Used by both the player and monsters. Spatial detection lives elsewhere
    /// (raycast/overlap/projectile); this only resolves the outcome.
    /// </summary>
    public sealed class HitReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;

        private Health _health;

        public float Max => maxHealth;
        public float Current => _health.Current;
        public bool IsDead => _health.IsDead;

        /// <summary>Fraction of incoming damage prevented (0..1) — set from the player's
        /// passive discoveries (Game.PassiveModifiers). 0 for monsters.</summary>
        public float DamageReduction { get; set; }

        /// <summary>(receiver, amount)</summary>
        public event Action<HitReceiver, float> Damaged;
        public event Action<HitReceiver> Died;

        private void Awake() => _health = Health.Full(maxHealth);

        /// <summary>Set max health at spawn (e.g. per monster tier) and refill.</summary>
        public void SetMaxHealth(float max)
        {
            maxHealth = max;
            _health = Health.Full(max);
        }

        public void TakeDamage(float amount, GameObject source)
        {
            if (IsDead) return;

            amount *= Mathf.Clamp01(1f - DamageReduction); // passive damage reduction
            _health = CombatResolver.ApplyDamage(_health, amount);
            Damaged?.Invoke(this, amount);
            if (IsDead) Died?.Invoke(this);
        }

        /// <summary>Restore health up to max (e.g. a skill's Leech self-heal).</summary>
        public void Heal(float amount)
        {
            _health = CombatResolver.ApplyHeal(_health, amount);
        }

        public void Revive()
        {
            _health = Health.Full(maxHealth);
        }
    }
}
