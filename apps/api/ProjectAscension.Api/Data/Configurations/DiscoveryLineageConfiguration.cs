using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class DiscoveryLineageConfiguration : IEntityTypeConfiguration<DiscoveryLineage>
{
    public void Configure(EntityTypeBuilder<DiscoveryLineage> builder)
    {
        builder.HasKey(e => new { e.ChildDiscoveryId, e.ParentDiscoveryId });
        builder.HasIndex(e => e.ChildDiscoveryId);

        // Restrict: lineage is a permanent historical record, never cascade-deleted,
        // and the two FKs to Discovery would otherwise create multiple cascade paths.
        builder.HasOne(e => e.Child).WithMany().HasForeignKey(e => e.ChildDiscoveryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Parent).WithMany().HasForeignKey(e => e.ParentDiscoveryId).OnDelete(DeleteBehavior.Restrict);
    }
}
