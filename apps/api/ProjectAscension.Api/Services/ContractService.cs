using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _repo;
    public ContractService(IContractRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<ContractResponse>>> GetByRegionAsync(Guid regionId, CancellationToken ct = default)
    {
        var contracts = await _repo.GetByRegionAsync(regionId, ct);
        var responses = (IReadOnlyList<ContractResponse>)contracts.Select(ToResponse).ToList();
        return Result<IReadOnlyList<ContractResponse>>.Ok(responses);
    }

    public async Task<Result<ContractResponse>> AcceptAsync(Guid contractId, AcceptContractRequest request, CancellationToken ct = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, ct);
        if (contract is null) return Result<ContractResponse>.Fail(Error.NotFound);
        if (contract.Status != ContractStatus.Open) return Result<ContractResponse>.Fail(Error.Conflict);

        contract.Status = ContractStatus.Assigned;
        contract.AssigneeActorId = request.ActorId;
        await _repo.UpdateAsync(contract, ct);
        return Result<ContractResponse>.Ok(ToResponse(contract));
    }

    public async Task<Result<ContractResponse>> CompleteAsync(Guid contractId, CancellationToken ct = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, ct);
        if (contract is null) return Result<ContractResponse>.Fail(Error.NotFound);
        if (contract.Status != ContractStatus.Assigned) return Result<ContractResponse>.Fail(Error.Conflict);

        contract.Status = ContractStatus.Completed;
        contract.CompletedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(contract, ct);
        return Result<ContractResponse>.Ok(ToResponse(contract));
    }

    private static ContractResponse ToResponse(Domain.Entities.Contract c) =>
        new(c.Id,
            (Contracts.Enums.ContractKind)(int)c.Kind,
            c.Title,
            c.Description,
            (Contracts.Enums.ContractStatus)(int)c.Status,
            c.RewardJson);
}
