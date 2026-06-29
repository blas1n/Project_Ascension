using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class CombatTuningSettingsConfiguration : IEntityTypeConfiguration<CombatTuningSettings>
{
    public void Configure(EntityTypeBuilder<CombatTuningSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever(); // fixed singleton row

        // Seeded defaults (mirror CombatTuning.Default) — editable at runtime.
        builder.HasData(new CombatTuningSettings
        {
            Id = 1,
            ProjectileDamage = 10f,
            BeamDamage = 9f,
            AreaDamage = 8f,
            DotDamagePerTick = 3f,
            SpreadFalloff = 0.6f,
            BaseDotTicks = 2,
            ShieldPerMagnitude = 12f,
            DashPerMagnitude = 2f,
            LeechFractionPerMagnitude = 0.15f,
            ControlDurationPerMagnitude = 0.6f,
            PassiveShieldReduction = 0.06f,
            PassiveBarrierReduction = 0.08f,
            PassiveLeech = 0.05f,
            FocusCostPerPoint = 4f,
            SlowPerMagnitude = 0.15f,
            KnockbackPerMagnitude = 4f,
            ChargedAttackThreshold = 0.7f,
            DeliveryProjectileSpeed = 32f,
            DeliveryProjectileGravity = 0f,
            DeliveryRange = 60f,
            DeliveryAreaRadius = 4f,
            DeliveryHitscanRadius = 1.5f,
        });
    }
}
