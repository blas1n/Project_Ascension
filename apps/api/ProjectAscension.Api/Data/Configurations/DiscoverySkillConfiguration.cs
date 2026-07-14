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
        builder.Property(s => s.Delivery).HasMaxLength(32).HasDefaultValue(string.Empty);

        builder.HasIndex(s => s.DiscoveryId).IsUnique();
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.IdempotencyKey).IsUnique();

        // Backstop, not the fix (the fix is SkillCompositionService.ComposePendingAsync's
        // check-and-retry against the taken graph/name sets) — a last line of defense against a
        // Ready row ever sharing another Ready row's exact composed structure, e.g. if the worker
        // is ever scaled to more than one instance and two passes race past the in-memory check.
        // Global, not per-discoverer: the vertical slice's dedup is already global (GetReadyAsync
        // has no actor filter — "Slice = one actor"), so this mirrors the SAME scope, not a wider
        // one. This must become actor-scoped (needs an ActorId on this table) before the MMO ships,
        // or two different players legitimately landing on the same minimal Common-tier shape would
        // 500 on insert instead of being the unremarkable coincidence it is.
        builder.HasIndex(s => s.EffectGraphJson)
            .IsUnique()
            .HasFilter("\"Status\" = 'Ready'")
            .HasDatabaseName("IX_DiscoverySkills_EffectGraphJson_UniqueWhenReady");

        builder.HasOne(s => s.Discovery).WithMany().HasForeignKey(s => s.DiscoveryId);
    }
}
