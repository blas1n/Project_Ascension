using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class DiscoveryConfiguration : IEntityTypeConfiguration<Discovery>
{
    public void Configure(EntityTypeBuilder<Discovery> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Type).HasConversion<string>();
        builder.Property(d => d.Title).IsRequired().HasMaxLength(200);
        builder.HasOne(d => d.Discoverer).WithMany().HasForeignKey(d => d.DiscovererActorId);
        builder.HasOne(d => d.Region).WithMany().HasForeignKey(d => d.RegionId);
    }
}
