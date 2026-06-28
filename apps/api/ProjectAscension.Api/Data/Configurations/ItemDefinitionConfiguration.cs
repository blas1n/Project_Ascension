using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class ItemDefinitionConfiguration : IEntityTypeConfiguration<ItemDefinition>
{
    public void Configure(EntityTypeBuilder<ItemDefinition> builder)
    {
        builder.HasKey(i => i.Key);
        builder.Property(i => i.Key).HasMaxLength(32).ValueGeneratedNever();
        builder.Property(i => i.DisplayName).HasMaxLength(64);

        // The monster-drop materials, priced for the city shop. Sellable (drops → gold)
        // and buyable (acquire materials for settlement supply). Editable at runtime.
        builder.HasData(
            new ItemDefinition { Key = "hide", DisplayName = "Beast Hide", SellPrice = 8, BuyPrice = 16 },
            new ItemDefinition { Key = "feather", DisplayName = "Sky Feather", SellPrice = 10, BuyPrice = 20 },
            new ItemDefinition { Key = "core", DisplayName = "Elite Core", SellPrice = 40, BuyPrice = 90 });
    }
}
