using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class DiscoverySkillConfiguration : IEntityTypeConfiguration<DiscoverySkill>
{
    public void Configure(EntityTypeBuilder<DiscoverySkill> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Theme).HasMaxLength(200);
        builder.Property(s => s.PrimaryBehavior).HasMaxLength(50);
        builder.Property(s => s.Name).HasMaxLength(200);
        builder.Property(s => s.Manifestation).HasMaxLength(20);
        builder.Property(s => s.IdempotencyKey).HasMaxLength(128);
        builder.Property(s => s.BehaviorProfileJson).HasDefaultValue("[]"); // existing rows backfill

        builder.HasIndex(s => s.DiscoveryId).IsUnique();
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.IdempotencyKey).IsUnique();

        builder.HasOne(s => s.Discovery).WithMany().HasForeignKey(s => s.DiscoveryId);
    }
}
