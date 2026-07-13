using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Api.Data.Configurations;

public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Type).HasConversion<string>();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);

        // The slice's single frontier region — WORLD data, not player data. The client (GameSession,
        // DiscoveryReporter) has always addressed this id; on a fresh database the row simply never
        // existed, so anything that FK's to it (Character.CurrentRegionId, Discovery.RegionId) 500'd.
        // Seeded here instead of created ad hoc so it exists before any character or discovery does.
        builder.HasData(new Region
        {
            Id = System.Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Frontier",
            Type = RegionType.Frontier,
            DangerLevel = 1,
            EnvironmentTagsJson = "[]",
        });
    }
}
