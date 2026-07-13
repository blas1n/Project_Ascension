#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>The completed contract plus the resulting authoritative player state (currency/
    /// reputation paid out). The contract is included so the client learns of any item reward
    /// (ADR 0014) without inventing one locally.</summary>
    public record ContractTurnInResponse(ContractResponse Contract, PlayerStateResponse PlayerState);
}
