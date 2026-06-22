using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Type).HasConversion<string>();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
    }
}
