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
            new ItemDefinition { Key = "hide", DisplayName = "Beast Hide", SellPrice = 8, BuyPrice = 16, Description = "Tough hide from frontier beasts — raises shelters and armor." },
            new ItemDefinition { Key = "feather", DisplayName = "Sky Feather", SellPrice = 10, BuyPrice = 20, Description = "Light feathers prized in the city markets." },
            new ItemDefinition { Key = "core", DisplayName = "Elite Core", SellPrice = 40, BuyPrice = 90, Description = "A volatile core torn from an elite — fortifies the outpost's defenses." },
            // Not a material — a possession. The survey pays in this, and a map is a thing you hold,
            // can lose, and can one day trade. Not sellable to the shop: it is not scrap.
            new ItemDefinition { Key = "frontier_map", DisplayName = "Frontier Map", SellPrice = 0, BuyPrice = 0, Description = "Charted ground beyond the wall. Ink, hide, and someone's survival." });
    }
}
