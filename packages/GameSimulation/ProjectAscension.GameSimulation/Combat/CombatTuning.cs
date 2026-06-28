namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// The tunable weights combat resolution scores against — per-magnitude damage,
    /// defensive conversions, control duration, and focus cost. The resolvers stay
    /// pure: the host loads these (from the DB, at runtime) and passes them in, so
    /// combat numbers are data-driven and server-authoritative, never hard-coded.
    /// Matters especially for the weapon-creation system — a discovered weapon's combat
    /// output is derived through these, so a balance edit reshapes every created weapon.
    /// </summary>
    // A record class (not record struct) so it compiles under Unity's C# 9.
    public sealed record CombatTuning(
        // Offensive (per magnitude).
        float ProjectileDamage,
        float BeamDamage,
        float AreaDamage,
        float DotDamagePerTick,
        float SpreadFalloff,      // chained/pierced targets take this fraction of the focused hit
        int BaseDotTicks,         // + duration tier
                                  // Defensive / utility (per magnitude).
        float ShieldPerMagnitude,
        float DashPerMagnitude,
        float LeechFractionPerMagnitude,
        float ControlDurationPerMagnitude, // seconds of slow/stun per control magnitude
                                           // Passive (always-on) conversions.
        float PassiveShieldReduction,
        float PassiveBarrierReduction,
        float PassiveLeech,
        // Resource.
        float FocusCostPerPoint,
        // Control strength per control magnitude — so the status the skill applies is
        // defined by the skill (its magnitude), not fixed on the receiver.
        float SlowPerMagnitude,      // slow fraction per magnitude (0.45 → move at 55%)
        float KnockbackPerMagnitude, // impulse speed per magnitude
                                     // Input.
        float ChargedAttackThreshold)// draw fraction (0..1) that counts as a charged shot
    {
        /// <summary>A baseline used by tests and as a safe fallback when the DB has no
        /// combat-tuning row yet. Mirrors the seeded defaults.</summary>
        public static CombatTuning Default { get; } = new(
            ProjectileDamage: 10f,
            BeamDamage: 9f,
            AreaDamage: 8f,
            DotDamagePerTick: 3f,
            SpreadFalloff: 0.6f,
            BaseDotTicks: 2,
            ShieldPerMagnitude: 12f,
            DashPerMagnitude: 2f,
            LeechFractionPerMagnitude: 0.15f,
            ControlDurationPerMagnitude: 0.6f,
            PassiveShieldReduction: 0.06f,
            PassiveBarrierReduction: 0.08f,
            PassiveLeech: 0.05f,
            FocusCostPerPoint: 4f,
            SlowPerMagnitude: 0.15f,
            KnockbackPerMagnitude: 4f,
            ChargedAttackThreshold: 0.7f);
    }
}
