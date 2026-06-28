using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Api.Data.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    // Fixed seed timestamp so the HasData rows are deterministic (no migration churn).
    private static readonly System.DateTime SeedTime = new(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Kind).HasConversion<string>();
        builder.Property(c => c.Purpose).HasConversion<string>();
        builder.Property(c => c.Status).HasConversion<string>();
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);

        // The slice's three contract types — the board fetches these (objective/reward
        // are simple numbers in the Conditions/Reward JSON). Editable at runtime; later a
        // dynamic/AI system can generate contracts into this same table.
        builder.HasData(
            new Contract
            {
                Id = System.Guid.Parse("c0000001-0000-0000-0000-000000000001"),
                Kind = ContractKind.Task,
                Purpose = ContractPurpose.Hunt,
                Status = ContractStatus.Open,
                Title = "Cull the Beasts",
                Description = "Defeat 5 monsters in the frontier.",
                ConditionsJson = "{\"targetCount\":5}",
                RewardJson = "{\"currency\":120}",
                CreatedAt = SeedTime,
            },
            new Contract
            {
                Id = System.Guid.Parse("c0000002-0000-0000-0000-000000000002"),
                Kind = ContractKind.Task,
                Purpose = ContractPurpose.Survey,
                Status = ContractStatus.Open,
                Title = "Map the Frontier",
                Description = "Reach the survey marker.",
                ConditionsJson = "{\"targetCount\":1}",
                RewardJson = "{\"currency\":80}",
                CreatedAt = SeedTime,
            },
            new Contract
            {
                Id = System.Guid.Parse("c0000003-0000-0000-0000-000000000003"),
                Kind = ContractKind.Task,
                Purpose = ContractPurpose.Collection,
                Status = ContractStatus.Open,
                Title = "Gather Samples",
                Description = "Collect 3 samples.",
                ConditionsJson = "{\"targetCount\":3}",
                RewardJson = "{\"currency\":90}",
                CreatedAt = SeedTime,
            },
            // A targeted hunt: only elite kills count (the objective filter, "외곽 늑대 5마리"
            // style). Higher reward for the harder, specific objective.
            new Contract
            {
                Id = System.Guid.Parse("c0000004-0000-0000-0000-000000000004"),
                Kind = ContractKind.Task,
                Purpose = ContractPurpose.Hunt,
                Status = ContractStatus.Open,
                Title = "Elite Bounty",
                Description = "Slay 2 elite monsters.",
                ConditionsJson = "{\"targetCount\":2,\"target\":\"elite\"}",
                RewardJson = "{\"currency\":220}",
                CreatedAt = SeedTime,
            },
            // Delegation tutorial (contract-example.md 위임 튜토리얼): deliberately too hard for
            // a starter — slay 4 elites, but the frontier spawns only one. The player learns
            // they can DELEGATE it (DelegationAllowed) rather than clear it alone.
            new Contract
            {
                Id = System.Guid.Parse("c0000005-0000-0000-0000-000000000005"),
                Kind = ContractKind.Task,
                Purpose = ContractPurpose.Hunt,
                Status = ContractStatus.Open,
                Title = "Deep Cull",
                Description = "Slay 4 elite monsters in the deep frontier. (Hard — consider delegating.)",
                ConditionsJson = "{\"targetCount\":4,\"target\":\"elite\"}",
                RewardJson = "{\"currency\":400}",
                DelegationAllowed = true,
                CreatedAt = SeedTime,
            });
    }
}
