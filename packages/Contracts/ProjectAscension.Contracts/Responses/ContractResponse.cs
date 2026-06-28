#nullable enable
using System;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Responses
{
    public record ContractResponse(
        Guid Id, ContractKind Kind, string Title, string Description, ContractStatus Status, string RewardJson,
        // Typed objective fields the slice board uses (parsed from the Conditions/Reward JSON).
        ContractPurpose Purpose, int TargetCount, int RewardCurrency);
}
