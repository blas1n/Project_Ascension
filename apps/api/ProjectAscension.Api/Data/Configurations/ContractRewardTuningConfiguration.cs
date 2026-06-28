using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class ContractRewardTuningConfiguration : IEntityTypeConfiguration<ContractRewardTuning>
{
    public void Configure(EntityTypeBuilder<ContractRewardTuning> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever(); // fixed singleton row

        builder.HasData(new ContractRewardTuning
        {
            Id = 1,
            BaseRewardPerCount = 25f,
            DifficultyScale = 0.4f,
            BandMinPercent = 70,
            BandMaxPercent = 150,
        });
    }
}
