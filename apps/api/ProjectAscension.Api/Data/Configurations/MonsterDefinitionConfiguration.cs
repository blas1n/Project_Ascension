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
                AttackWindup = 0.35f, // quick tell — a fast, low-damage swing
                Damage = 8f,
                ProjectileSpeed = 0f,
                Scale = 1f,
                DropItemKey = "hide",
                DropAmount = 2,
            },
            new MonsterDefinition
            {
                Key = "ranged",
                MaxHealth = 25f,
                MoveSpeed = 2f,
                AggroRange = 30f,
                AttackRange = 14f,
                AttackCooldown = 1.5f,
                AttackWindup = 0.5f, // an aim/charge tell before the shot
                Damage = 6f,
                ProjectileSpeed = 18f,
                Scale = 1f,
                DropItemKey = "feather",
                DropAmount = 2,
            },
            new MonsterDefinition
            {
                Key = "elite",
                MaxHealth = 120f,
                MoveSpeed = 2.5f,
                AggroRange = 35f,
                AttackRange = 18f,
                AttackCooldown = 1.2f,
                AttackWindup = 0.65f, // a heavy, well-signalled blow — most reactable
                Damage = 14f,
                ProjectileSpeed = 24f,
                Scale = 1.6f,
                DropItemKey = "core",
                DropAmount = 1,
            },
            // The thing in the deep. It is not balanced to be beaten by a starter loadout — it is
            // balanced to TEACH (stage 8). The long wind-up is deliberate: the blow is readable, and
            // the player will still take it, because readable is not the same as survivable.
            new MonsterDefinition
            {
                Key = "guardian",
                MaxHealth = 600f,
                MoveSpeed = 3.2f,
                AggroRange = 45f,
                AttackRange = 20f,
                AttackCooldown = 1.4f,
                AttackWindup = 0.9f,
                Damage = 45f,
                ProjectileSpeed = 26f,
                Scale = 2.6f,
                DropItemKey = "core",
                DropAmount = 3,
            });
    }
}
