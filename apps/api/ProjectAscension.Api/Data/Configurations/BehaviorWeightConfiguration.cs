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
        builder.HasData(
            new BehaviorWeight { Behavior = "Jump", Weight = 1 },
            new BehaviorWeight { Behavior = "Dodge", Weight = 1 },
            new BehaviorWeight { Behavior = "MeleeAttack", Weight = 2 },
            new BehaviorWeight { Behavior = "RangedAttack", Weight = 2 },
            new BehaviorWeight { Behavior = "ChargeAttack", Weight = 3 },
            new BehaviorWeight { Behavior = "DodgeAttack", Weight = 3 });
    }
}
