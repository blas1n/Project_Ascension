using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class EconomyTuningConfiguration : IEntityTypeConfiguration<EconomyTuning>
{
    public void Configure(EntityTypeBuilder<EconomyTuning> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever(); // fixed singleton row

        builder.HasData(new EconomyTuning
        {
            Id = 1,
            KnowledgeGoldPerPoint = 6,
            KnowledgePointsPerRep = 5,
        });
    }
}
