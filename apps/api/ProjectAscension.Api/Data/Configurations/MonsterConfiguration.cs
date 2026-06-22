using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class MonsterConfiguration : IEntityTypeConfiguration<Monster>
{
    public void Configure(EntityTypeBuilder<Monster> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tier).HasConversion<string>();
        builder.HasOne(m => m.Species).WithMany().HasForeignKey(m => m.SpeciesId);
        builder.HasOne(m => m.Region).WithMany().HasForeignKey(m => m.RegionId);
    }
}
