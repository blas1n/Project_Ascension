using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class DiscoveryProgressConfiguration : IEntityTypeConfiguration<DiscoveryProgress>
{
    public void Configure(EntityTypeBuilder<DiscoveryProgress> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => new { d.ActorId, d.DiscoveryCandidateId }).IsUnique();
        builder.HasOne(d => d.Actor).WithMany().HasForeignKey(d => d.ActorId);
        builder.HasOne(d => d.Candidate).WithMany().HasForeignKey(d => d.DiscoveryCandidateId);
    }
}
