using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class MonsterSpeciesConfiguration : IEntityTypeConfiguration<MonsterSpecies>
{
    public void Configure(EntityTypeBuilder<MonsterSpecies> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Tier).HasConversion<string>();
        builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
    }
}
