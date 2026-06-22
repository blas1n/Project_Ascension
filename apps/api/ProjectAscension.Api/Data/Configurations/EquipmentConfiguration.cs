using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.HasKey(e => e.ItemId);
        builder.Property(e => e.EquipmentType).HasConversion<string>();
        builder.Property(e => e.SlotType).HasConversion<string>();
        builder.HasOne(e => e.Item).WithOne()
            .HasForeignKey<Equipment>(e => e.ItemId);
    }
}
