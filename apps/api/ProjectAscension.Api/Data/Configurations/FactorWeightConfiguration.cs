using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class FactorWeightConfiguration : IEntityTypeConfiguration<FactorWeight>
{
    public void Configure(EntityTypeBuilder<FactorWeight> builder)
    {
        builder.HasKey(f => f.Key);
        builder.Property(f => f.Key).HasMaxLength(50);
        builder.Property(f => f.Category).HasMaxLength(20);

        // Seeded defaults (mirror DiscoveryTuning.Default) — designers edit/add rows.
        builder.HasData(
            new FactorWeight { Key = "waterfall", Category = "Environment", Weight = 10 },
            new FactorWeight { Key = "ice_wall", Category = "Environment", Weight = 10 },
            new FactorWeight { Key = "crystal_desert", Category = "Environment", Weight = 12 },
            new FactorWeight { Key = "jungle", Category = "Environment", Weight = 8 },
            new FactorWeight { Key = "sword", Category = "Equipment", Weight = 4 },
            new FactorWeight { Key = "bow", Category = "Equipment", Weight = 4 },
            new FactorWeight { Key = "pistol", Category = "Equipment", Weight = 4 },
            new FactorWeight { Key = "catalyst", Category = "Equipment", Weight = 6 },
            new FactorWeight { Key = "fire", Category = "Knowledge", Weight = 8 },
            new FactorWeight { Key = "compression", Category = "Knowledge", Weight = 8 },
            new FactorWeight { Key = "wind", Category = "Knowledge", Weight = 8 });
    }
}
