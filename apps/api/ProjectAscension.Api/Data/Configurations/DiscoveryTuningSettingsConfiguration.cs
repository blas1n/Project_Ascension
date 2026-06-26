using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Data.Configurations;

public class DiscoveryTuningSettingsConfiguration : IEntityTypeConfiguration<DiscoveryTuningSettings>
{
    public void Configure(EntityTypeBuilder<DiscoveryTuningSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever(); // fixed singleton row

        // Seeded defaults (mirror DiscoveryTuning.Default) — editable at runtime.
        builder.HasData(new DiscoveryTuningSettings
        {
            Id = 1,
            DefaultBehaviorWeight = 1,
            DefaultFactorWeight = 0,
            KnowledgeDepthWeight = 12,
            PersistenceWeight = 5,
            CombinationSynergy = 15,
            FireThreshold = 100,
            BudgetBase = 8,
            BudgetPerScore = 0.18,
            BudgetMin = 16,
            BudgetMax = 64,
            UncommonScore = 120,
            RareScore = 150,
            EpicScore = 200,
            LegendaryScore = 250,
        });
    }
}
