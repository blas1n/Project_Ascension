using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface IContractService
{
    Task<Result<IReadOnlyList<ContractResponse>>> GetByRegionAsync(Guid regionId, CancellationToken ct = default);
    Task<Result<ContractQuoteResponse>> GetQuoteAsync(Domain.Enums.ContractPurpose purpose, string? target, int count, CancellationToken ct = default);

    /// <summary>Issue a contract; the reward is escrowed from the issuer's currency immediately
    /// (ADR 0014) — rejects when the issuer can't afford the calibrated reward.</summary>
    Task<Result<IssueContractResponse>> IssueAsync(IssueContractRequest request, CancellationToken ct = default);
    Task<Result<ContractResponse>> AcceptAsync(Guid contractId, AcceptContractRequest request, CancellationToken ct = default);
    Task<Result<ContractResponse>> CompleteAsync(Guid contractId, CancellationToken ct = default);
    Task<Result<ContractResponse>> UpdateProgressAsync(Guid contractId, UpdateContractProgressRequest request, CancellationToken ct = default);

    /// <summary>Hand in a completed contract — pays the reward from the contract's OWN stored
    /// terms and marks it Completed. Rejects when the assignee's reported progress hasn't
    /// reached the objective yet (ADR 0014).</summary>
    Task<Result<ContractTurnInResponse>> TurnInAsync(Guid contractId, TurnInContractRequest request, CancellationToken ct = default);

    /// <summary>Hand the active contract to a stub contractor instead of clearing it — escrows
    /// the reward as the contractor's fee. Rejects when the assignee can't afford it.</summary>
    Task<Result<PlayerStateResponse>> DelegateAsync(Guid contractId, DelegateContractRequest request, CancellationToken ct = default);

    /// <summary>Report a contract failure (died / deadline expired). The server — never the
    /// client — computes the reputation penalty from the contract's own stored reward via
    /// ContractRules.ReputationPenalty and moves it to Failed. Rejects when the contract isn't
    /// Assigned, isn't the caller's, or was already resolved (ADR 0014).</summary>
    Task<Result<PlayerStateResponse>> FailAsync(Guid contractId, FailContractRequest request, CancellationToken ct = default);
}
