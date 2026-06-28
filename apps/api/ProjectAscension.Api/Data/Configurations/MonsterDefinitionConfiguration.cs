using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class MonsterDefinitionConfiguration : IEntityTypeConfiguration<MonsterDefinition>
{
    public void Configure(EntityTypeBuilder<MonsterDefinition> builder)
    {
        builder.HasKey(m => m.Key);
        builder.Property(m => m.Key).HasMaxLength(32).ValueGeneratedNever();

        // Seeded starters (mirror MonsterFactory) — editable at runtime.
        builder.HasData(
            new MonsterDefinition
            {
                Key = "melee",
                MaxHealth = 40f,
                MoveSpeed = 3.5f,
                AggroRange = 25f,
                AttackRange = 2f,
                AttackCooldown = 1f,
                Damage = 8f,
                ProjectileSpeed = 0f,
                Scale = 1f,
            },
            new MonsterDefinition
            {
                Key = "ranged",
                MaxHealth = 25f,
                MoveSpeed = 2f,
                AggroRange = 30f,
                AttackRange = 14f,
                AttackCooldown = 1.5f,
                Damage = 6f,
                ProjectileSpeed = 18f,
                Scale = 1f,
            },
            new MonsterDefinition
            {
                Key = "elite",
                MaxHealth = 120f,
                MoveSpeed = 2.5f,
                AggroRange = 35f,
                AttackRange = 18f,
                AttackCooldown = 1.2f,
                Damage = 14f,
                ProjectileSpeed = 24f,
                Scale = 1.6f,
            });
    }
}
