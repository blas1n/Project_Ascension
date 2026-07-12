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

        /// <summary>Optional check: when it returns true the next hit is fully negated (the player's
        /// dodge i-frames). Null (monsters) = never invulnerable. The window itself is a headless
        /// combat rule (PlayerSimulation.IsInvulnerable); this only gates damage on it.</summary>
        public Func<bool> Invulnerable { get; set; }

        /// <summary>(receiver, amount)</summary>
        public event Action<HitReceiver, float> Damaged;
        public event Action<HitReceiver> Died;

        /// <summary>A hit was fully negated by invulnerability (i-frames) — for the dodge shine.</summary>
        public event Action<HitReceiver> DamageNegated;

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

            // I-frames (a dodge) fully negate the hit — the reward for a well-timed dodge.
            if (Invulnerable != null && Invulnerable())
            {
                DamageNegated?.Invoke(this);
                return;
            }

            // Passive damage reduction is applied by the resolver (tested, server-authoritative).
            float dealt = CombatResolver.Reduced(amount, DamageReduction);
            _health = CombatResolver.ApplyDamage(_health, dealt);
            Damaged?.Invoke(this, dealt);
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
