using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class FactorWeightConfiguration : IEntityTypeConfiguration<FactorWeight>
{
    // Seeded defaults (mirror DiscoveryTuning.Default) — designers edit/add rows. Exposed as a plain
    // array (not just inline in Configure) so a test can cross-check the keys against the game's
    // actual tag vocabulary without going through EF Core model-building — see
    // FactorAndBehaviorVocabularyTests, which pins the invariant that cost this project a live bug:
    // "sword"/"pistol"/"catalyst" were seeded here for years but EquipmentTags/SkillBinding only ever
    // emit "melee"/"firearm"/"bow"/"arcane", so those rows could never match anything the game sent.
    // Weights below were carried over 1:1 by the starter weapon each category belongs to
    // (Sword→melee, Pistol→firearm, Catalyst→arcane); Bow already matched and is unchanged.
    public static readonly FactorWeight[] Seed =
    {
        new FactorWeight { Key = "waterfall", Category = "Environment", Weight = 10 },
        new FactorWeight { Key = "ice_wall", Category = "Environment", Weight = 10 },
        new FactorWeight { Key = "crystal_desert", Category = "Environment", Weight = 12 },
        new FactorWeight { Key = "jungle", Category = "Environment", Weight = 8 },
        new FactorWeight { Key = "melee", Category = "Equipment", Weight = 4 },
        new FactorWeight { Key = "bow", Category = "Equipment", Weight = 4 },
        new FactorWeight { Key = "firearm", Category = "Equipment", Weight = 4 },
        new FactorWeight { Key = "arcane", Category = "Equipment", Weight = 6 },
        new FactorWeight { Key = "fire", Category = "Knowledge", Weight = 8 },
        new FactorWeight { Key = "compression", Category = "Knowledge", Weight = 8 },
        new FactorWeight { Key = "wind", Category = "Knowledge", Weight = 8 },
        new FactorWeight { Key = "monster:melee", Category = "Monster", Weight = 6 },
        new FactorWeight { Key = "monster:ranged", Category = "Monster", Weight = 8 },
        new FactorWeight { Key = "monster:elite", Category = "Monster", Weight = 14 },
    };

    public void Configure(EntityTypeBuilder<FactorWeight> builder)
    {
        builder.HasKey(f => f.Key);
        builder.Property(f => f.Key).HasMaxLength(50);
        builder.Property(f => f.Category).HasMaxLength(20);
        builder.HasData(Seed);
    }
}
