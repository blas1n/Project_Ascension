#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// The single-row tunable weights for combat resolution — per-magnitude damage,
    /// defensive conversions, control duration, and focus cost. Server-managed balance
    /// data, editable at runtime (no redeploy). Whoever runs the resolvers reads these,
    /// so a discovered (created) weapon's combat output is fully data-driven — a balance
    /// edit reshapes every created weapon without code changes.
    /// </summary>
    public class CombatTuningSettings
    {
        public int Id { get; set; } // fixed singleton key (1)

        // Offensive (per magnitude).
        public float ProjectileDamage { get; set; }
        public float BeamDamage { get; set; }
        public float AreaDamage { get; set; }
        public float DotDamagePerTick { get; set; }
        public float SpreadFalloff { get; set; }
        public int BaseDotTicks { get; set; }

        // Defensive / utility (per magnitude).
        public float ShieldPerMagnitude { get; set; }
        public float DashPerMagnitude { get; set; }
        public float LeechFractionPerMagnitude { get; set; }
        public float ControlDurationPerMagnitude { get; set; }

        // Passive (always-on) conversions.
        public float PassiveShieldReduction { get; set; }
        public float PassiveBarrierReduction { get; set; }
        public float PassiveLeech { get; set; }

        // Resource.
        public float FocusCostPerPoint { get; set; }

        // Control strength per control magnitude (the skill defines the status numbers).
        public float SlowPerMagnitude { get; set; }
        public float KnockbackPerMagnitude { get; set; }

        // Input.
        public float ChargedAttackThreshold { get; set; }

        // Delivery (manifestation numbers; AI picks the concept, the engine owns these).
        public float DeliveryProjectileSpeed { get; set; }
        public float DeliveryProjectileGravity { get; set; }
        public float DeliveryRange { get; set; }
        public float DeliveryAreaRadius { get; set; }
        public float DeliveryHitscanRadius { get; set; }

        // Active block (shield): held, not passive, and only covering the front arc.
        public float BlockReduction { get; set; }
        public float BlockFrontArcDot { get; set; }

        // Discovery grammar input (ADR 0009): metres of per-frame displacement that reads as Moving.
        public float MovingDistanceThreshold { get; set; }

        // Binding lock: seconds since the player's last combat activity before the discovery
        // journal's hotkey binder opens back up (ADR: binding is knowledge, not equipment).
        public float BindingCombatLockSeconds { get; set; }
    }
}
