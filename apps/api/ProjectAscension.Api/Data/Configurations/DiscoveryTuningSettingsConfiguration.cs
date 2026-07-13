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
            // DiscoveryScarcity: trimmed from 15 — a bare few distinct behaviour/factor KINDS touched
            // in one brief window must not outweigh the fusion itself (ADR 0010).
            CombinationSynergy = 10,
            // The grammar's operators (ADR 0009), by how deliberate the act is. A fusion is worth more
            // than a dozen shots; repetition is worth the least, because it is the easiest thing a
            // player can do and must not be the road to every discovery.
            FuseWeight = 25,
            SequenceWeight = 15,
            ConcurrencyWeight = 12,
            ChainWeight = 6,
            // DiscoveryScarcity: raised from 100 — one spell cast fused into a short mag-dump must not
            // clear the bar for a first discovery (ADR 0010; see DiscoveryTuning.Default for the
            // arithmetic). Rarity bands rescaled to match, same ×1.5 exponential spacing.
            FireThreshold = 200,
            BudgetBase = 6,
            // Cost exponential, power logarithmic (ADR 0010): grinding one act exhausts itself, and
            // getting stronger stays possible but grows steadily dearer.
            BudgetGrowth = 2.4,
            BudgetMin = 10,
            BudgetMax = 40,
            UncommonScore = 300,
            RareScore = 450,
            EpicScore = 675,
            LegendaryScore = 1013,
        });
    }
}
