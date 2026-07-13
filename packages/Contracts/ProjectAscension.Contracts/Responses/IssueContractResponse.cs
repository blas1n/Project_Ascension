#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>A newly-issued contract plus the resulting authoritative player state — issuing
    /// escrows the reward from the issuer's currency (ADR 0014), so the client needs both.</summary>
    public record IssueContractResponse(ContractResponse Contract, PlayerStateResponse PlayerState);
}
