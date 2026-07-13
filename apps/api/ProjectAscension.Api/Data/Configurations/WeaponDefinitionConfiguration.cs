using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Api.Data.Configurations;

public class WeaponDefinitionConfiguration : IEntityTypeConfiguration<WeaponDefinition>
{
    public void Configure(EntityTypeBuilder<WeaponDefinition> builder)
    {
        builder.HasKey(w => w.Key);
        builder.Property(w => w.Key).HasMaxLength(32).ValueGeneratedNever();
        builder.Property(w => w.DisplayName).HasMaxLength(64);

        // Seeded starters (mirror ProjectAscensionSetup) — editable at runtime.
        builder.HasData(
            new WeaponDefinition
            {
                Key = "sword",
                DisplayName = "Sword",
                EquipmentType = EquipmentType.Weapon,
                SlotType = SlotType.Either,
                Damage = 25f,
                Range = 2.2f,
                ProjectileSpeed = 0f,
                Cooldown = 0.5f,
                MaxChargeMultiplier = 1f,
            },
            new WeaponDefinition
            {
                Key = "bow",
                DisplayName = "Bow",
                EquipmentType = EquipmentType.Bow,
                SlotType = SlotType.TwoHand,
                Damage = 18f,
                Range = 60f,
                ProjectileSpeed = 28f,
                ProjectileGravity = 9.8f,
                Cooldown = 0.6f,
                ChargeTime = 0.8f,
                MaxChargeMultiplier = 2.5f,
            },
            new WeaponDefinition
            {
                Key = "pistol",
                DisplayName = "Pistol",
                EquipmentType = EquipmentType.Firearm,
                SlotType = SlotType.Either,
                Damage = 12f,
                Range = 60f,
                ProjectileSpeed = 0f,
                Cooldown = 0.25f,
                MaxChargeMultiplier = 1f,
                SpreadMin = 1f,
                SpreadMax = 9f,
                SpreadPerShot = 1.4f,
                SpreadRecovery = 7f,
                // A magazine, not an ammo economy: running dry costs a beat of vulnerability (the
                // reload) — there is no reserve to track. Everything else is 0 (no magazine, never reloads).
                MagazineSize = 12,
                ReloadTime = 1.6f,
            },
            new WeaponDefinition
            {
                Key = "catalyst",
                DisplayName = "Arcane Catalyst",
                EquipmentType = EquipmentType.Catalyst,
                SlotType = SlotType.Either,
                Damage = 22f,
                Range = 50f,
                ProjectileSpeed = 18f,
                Cooldown = 0.8f,
                MaxChargeMultiplier = 1f,
            },
            // A shield is not a weapon: it deals nothing and fires nothing. Held in the off hand, it
            // BLOCKS while the hand is held down (BlockRules) — an active decision, not a passive stat.
            new WeaponDefinition
            {
                Key = "shield",
                DisplayName = "Kite Shield",
                EquipmentType = EquipmentType.Shield,
                SlotType = SlotType.Left,
                Damage = 0f,
                Range = 0f,
                ProjectileSpeed = 0f,
                Cooldown = 0f,
                MaxChargeMultiplier = 1f,
            });
    }
}
