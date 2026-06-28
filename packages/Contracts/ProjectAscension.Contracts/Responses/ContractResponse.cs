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
        ContractPurpose Purpose, int TargetCount, int RewardCurrency, string? Target, bool DelegationAllowed,
        // Reputation (명성) granted on completion, and the standing required to accept
        // (0 = open to all). The reputation loop: do contracts → gain standing → unlock harder ones.
        int RewardReputation, int MinReputation);
}
