#nullable enable
using System;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>A player issuing a contract: they choose the objective and how generous
    /// the reward is; the server calibrates/validates the reward and fills the title and
    /// description. Title/Description are optional (auto-generated when blank).</summary>
    public record IssueContractRequest(
        Guid IssuerActorId,
        ContractPurpose Purpose,
        string? Target,
        int TargetCount,
        int DesiredReward,
        int DurationHours,
        string? Title = null,
        string? Description = null);
}
