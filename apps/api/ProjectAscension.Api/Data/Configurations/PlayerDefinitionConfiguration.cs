using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class PlayerDefinitionConfiguration : IEntityTypeConfiguration<PlayerDefinition>
{
    public void Configure(EntityTypeBuilder<PlayerDefinition> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever(); // fixed singleton row

        // Seeded defaults (mirror PlayerData / FocusPool / HitReceiver) — editable at runtime.
        builder.HasData(new PlayerDefinition
        {
            Id = 1,
            MaxHealth = 100f,
            MoveSpeed = 5f,
            JumpVelocity = 6f,
            Gravity = 20f,
            DodgeSpeed = 12f,
            DodgeDuration = 0.2f,
            MaxFocus = 100f,
            FocusRegenPerSecond = 15f,
        });
    }
}
