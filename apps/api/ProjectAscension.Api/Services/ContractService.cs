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

    public async Task<Result<ContractResponse>> UpdateProgressAsync(Guid contractId, UpdateContractProgressRequest request, CancellationToken ct = default)
    {
        var contract = await _repo.GetByIdAsync(contractId, ct);
        if (contract is null) return Result<ContractResponse>.Fail(Error.NotFound);
        if (contract.Status != ContractStatus.Assigned) return Result<ContractResponse>.Fail(Error.Conflict);
        if (contract.AssigneeActorId != request.ActorId) return Result<ContractResponse>.Fail(Error.Conflict);

        contract.ProgressCount = request.ProgressCount;
        await _repo.UpdateAsync(contract, ct);
        return Result<ContractResponse>.Ok(ToResponse(contract));
    }

    private static ContractResponse ToResponse(Domain.Entities.Contract c) =>
        new(c.Id, c.Kind, c.Title, c.Description, c.Status, c.RewardJson, c.Purpose,
            ReadInt(c.ConditionsJson, "targetCount", 1), ReadInt(c.RewardJson, "currency", 0));

    // The slice's objective/reward are simple numbers in the Conditions/Reward JSON.
    private static int ReadInt(string json, string property, int fallback)
    {
        if (string.IsNullOrWhiteSpace(json)) return fallback;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(property, out var v) && v.TryGetInt32(out var n) ? n : fallback;
        }
        catch (System.Text.Json.JsonException)
        {
            return fallback;
        }
    }
}
