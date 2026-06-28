#nullable enable
using System;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Responses
{
    public record ContractResponse(
        Guid Id, ContractKind Kind, string Title, string Description, ContractStatus Status, string RewardJson,
        // Typed objective fields the slice board uses (parsed from the Conditions/Reward JSON).
        // Target is an optional objective filter (e.g. a monster key "elite" for a targeted
        // hunt); null/empty means "any".
        ContractPurpose Purpose, int TargetCount, int RewardCurrency, string? Target);
}
