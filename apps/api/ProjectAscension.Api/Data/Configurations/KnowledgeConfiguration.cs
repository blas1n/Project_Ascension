using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class KnowledgeConfiguration : IEntityTypeConfiguration<Knowledge>
{
    public void Configure(EntityTypeBuilder<Knowledge> builder)
    {
        builder.HasKey(k => k.Id);
        builder.HasIndex(k => k.OwnerActorId);
        builder.HasIndex(k => k.DiscoveryId).IsUnique();

        builder.HasOne(k => k.Discovery).WithMany().HasForeignKey(k => k.DiscoveryId);
        builder.HasOne(k => k.Owner).WithMany().HasForeignKey(k => k.OwnerActorId);
    }
}
