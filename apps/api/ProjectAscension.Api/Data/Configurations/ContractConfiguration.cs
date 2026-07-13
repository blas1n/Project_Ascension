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
                ConditionsJson = "{\"targetCount\":5,\"issuer\":\"City Watch\"}",
                RewardJson = "{\"currency\":120,\"reputation\":5}",
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
                ConditionsJson = "{\"targetCount\":1,\"issuer\":\"Survey Office\"}",
                // The first hour's survey pays in a MAP, not gold — the player owns it, and one day can
                // lose or trade it (docs/03-gameplay/first-hour-experience.md).
                RewardJson = "{\"currency\":20,\"reputation\":4,\"itemKey\":\"frontier_map\",\"itemAmount\":1}",
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
                ConditionsJson = "{\"targetCount\":3,\"issuer\":\"Herbalist Guild\"}",
                RewardJson = "{\"currency\":90,\"reputation\":3}",
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
                ConditionsJson = "{\"targetCount\":2,\"target\":\"elite\",\"timeLimitSeconds\":120,\"failOn\":[\"timeout\"],\"issuer\":\"Bounty Office\"}",
                RewardJson = "{\"currency\":220,\"reputation\":8}",
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
                ConditionsJson = "{\"targetCount\":4,\"target\":\"elite\",\"issuer\":\"Frontier Command\"}",
                RewardJson = "{\"currency\":400,\"reputation\":12}",
                DelegationAllowed = true,
                CreatedAt = SeedTime,
            },
            // Reputation-gated high tier: only an established expeditionary may accept it.
            // Closes the loop — basic contracts build the standing that unlocks this one.
            new Contract
            {
                Id = System.Guid.Parse("c0000006-0000-0000-0000-000000000006"),
                Kind = ContractKind.Task,
                Purpose = ContractPurpose.Hunt,
                Status = ContractStatus.Open,
                Title = "Frontier Warden",
                Description = "A trusted expeditionary's charge: slay 3 elites. (Requires standing.)",
                ConditionsJson = "{\"targetCount\":3,\"target\":\"elite\",\"minReputation\":20,\"timeLimitSeconds\":180,\"failOn\":[\"timeout\",\"death\"],\"issuer\":\"Warden\u0027s Office\"}",
                RewardJson = "{\"currency\":350,\"reputation\":15}",
                CreatedAt = SeedTime,
            });
    }
}
