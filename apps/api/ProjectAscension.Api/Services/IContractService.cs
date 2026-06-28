using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface IContractService
{
    Task<Result<IReadOnlyList<ContractResponse>>> GetByRegionAsync(Guid regionId, CancellationToken ct = default);
    Task<Result<ContractQuoteResponse>> GetQuoteAsync(Domain.Enums.ContractPurpose purpose, string? target, int count, CancellationToken ct = default);
    Task<Result<ContractResponse>> IssueAsync(IssueContractRequest request, CancellationToken ct = default);
    Task<Result<ContractResponse>> AcceptAsync(Guid contractId, AcceptContractRequest request, CancellationToken ct = default);
    Task<Result<ContractResponse>> CompleteAsync(Guid contractId, CancellationToken ct = default);
    Task<Result<ContractResponse>> UpdateProgressAsync(Guid contractId, UpdateContractProgressRequest request, CancellationToken ct = default);
}
