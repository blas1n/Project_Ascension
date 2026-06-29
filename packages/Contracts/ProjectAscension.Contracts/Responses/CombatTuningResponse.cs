#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>The current combat balance values (read-only view), for inspection and
    /// verifying runtime edits to the combat-tuning row.</summary>
    public record CombatTuningResponse(
        float ProjectileDamage,
        float BeamDamage,
        float AreaDamage,
        float DotDamagePerTick,
        float SpreadFalloff,
        int BaseDotTicks,
        float ShieldPerMagnitude,
        float DashPerMagnitude,
        float LeechFractionPerMagnitude,
        float ControlDurationPerMagnitude,
        float PassiveShieldReduction,
        float PassiveBarrierReduction,
        float PassiveLeech,
        float FocusCostPerPoint,
        float SlowPerMagnitude,
        float KnockbackPerMagnitude,
        float ChargedAttackThreshold,
        float DeliveryProjectileSpeed,
        float DeliveryProjectileGravity,
        float DeliveryRange,
        float DeliveryAreaRadius,
        float DeliveryHitscanRadius);
}
