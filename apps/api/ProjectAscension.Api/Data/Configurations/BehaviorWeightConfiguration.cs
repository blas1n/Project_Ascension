using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class BehaviorWeightConfiguration : IEntityTypeConfiguration<BehaviorWeight>
{
    public void Configure(EntityTypeBuilder<BehaviorWeight> builder)
    {
        builder.HasKey(w => w.Behavior);
        builder.Property(w => w.Behavior).HasMaxLength(50);

        // Seeded defaults — balance designers edit/add rows at runtime.
        // Only the RAW verbs are seeded now: what was done, how many times. Every composite behaviour
        // (a fusion, an air attack, a chained jump) is scored by PREFIX instead
        // (ADR 0009), so a combination nobody enumerated needs no row here.
        builder.HasData(
            new BehaviorWeight { Behavior = "Jump", Weight = 1 },
            new BehaviorWeight { Behavior = "MeleeAttack", Weight = 2 },
            new BehaviorWeight { Behavior = "RangedAttack", Weight = 2 });
    }
}
