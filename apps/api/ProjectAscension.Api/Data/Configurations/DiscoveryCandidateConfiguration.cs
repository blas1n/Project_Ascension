using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class DiscoveryCandidateConfiguration : IEntityTypeConfiguration<DiscoveryCandidate>
{
    public void Configure(EntityTypeBuilder<DiscoveryCandidate> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.CandidateKey).IsUnique();
        builder.Property(d => d.CandidateKey).IsRequired().HasMaxLength(200);
    }
}
