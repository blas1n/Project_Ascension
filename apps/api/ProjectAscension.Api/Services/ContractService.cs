using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public class ContractService : IContractService
{
    private const int MaxObjectiveCount = 20; // a sane upper bound for a slice contract

    private readonly IContractRepository _repo;
    private readonly IMonsterDefinitionRepository _monsters;
    private readonly IContractFlavorComposer _flavor;
    public ContractService(IContractRepository repo, IMonsterDefinitionRepository monsters, IContractFlavorComposer flavor)
    {
        _repo = repo;
        _monsters = monsters;
        _flavor = flavor;
    }

    public async Task<Result<IReadOnlyList<ContractResponse>>> GetByRegionAsync(Guid regionId, CancellationToken ct = default)
    {
        var contracts = await _repo.GetByRegionAsync(regionId, ct);
        var responses = (IReadOnlyList<ContractResponse>)contracts.Select(ToResponse).ToList();
        return Result<IReadOnlyList<ContractResponse>>.Ok(responses);
    }

    public async Task<Result<ContractQuoteResponse>> GetQuoteAsync(ContractPurpose purpose, string? target, int count, CancellationToken ct = default)
    {
        var (suggested, min, max) = await ComputeQuoteAsync(purpose, target, count, ct);
        return Result<ContractQuoteResponse>.Ok(new ContractQuoteResponse(suggested, min, max));
    }

    public async Task<Result<ContractResponse>> IssueAsync(IssueContractRequest request, CancellationToken ct = default)
    {
        if (request.IssuerActorId == Guid.Empty) return Result<ContractResponse>.Fail(Error.Invalid);

        int count = Math.Clamp(request.TargetCount, 1, MaxObjectiveCount);
        var (_, min, max) = await ComputeQuoteAsync(request.Purpose, request.Target, count, ct);
        int reward = Math.Clamp(request.DesiredReward, min, max); // the server owns the economy

        // Assisted: fill the tedious copy when the player didn't write it. The AI flavor
        // composer writes a posting from the objective (deterministic template under Stub /
        // CI); explicit player text always wins. Numbers stay deterministic (ADR 0002).
        bool authored = !string.IsNullOrWhiteSpace(request.Title) || !string.IsNullOrWhiteSpace(request.Description);
        ContractFlavor flavor = authored
            ? new ContractFlavor(
                string.IsNullOrWhiteSpace(request.Title) ? AutoTitle(request.Purpose, request.Target, count) : request.Title!.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? AutoDescription(request.Purpose, request.Target, count) : request.Description!.Trim())
            : await _flavor.ComposeAsync(request.Purpose, request.Target, count,
                AutoTitle(request.Purpose, request.Target, count), AutoDescription(request.Purpose, request.Target, count), ct);
        string title = flavor.Title;
        string description = flavor.Description;
        bool targeted = request.Purpose == ContractPurpose.Hunt && !string.IsNullOrEmpty(request.Target);

        var contract = new Domain.Entities.Contract
        {
            Id = Guid.NewGuid(),
            Kind = ContractKind.Task,
            Purpose = request.Purpose,
            Status = ContractStatus.Open,
            IssuerActorId = request.IssuerActorId,
            Title = title,
            Description = description,
            ConditionsJson = targeted ? $"{{\"targetCount\":{count},\"target\":\"{request.Target}\"}}" : $"{{\"targetCount\":{count}}}",
            RewardJson = $"{{\"currency\":{reward}}}",
            DelegationAllowed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.DurationHours > 0 ? DateTime.UtcNow.AddHours(request.DurationHours) : null,
        };
        await _repo.AddAsync(contract, ct);
        return Result<ContractResponse>.Ok(ToResponse(contract));
    }

    // Calibrate the reward from the objective: a flat base per unit plus, for a targeted
    // hunt, the monster's DB-driven difficulty (so harder targets pay more). The band lets
    // the issuer choose how generous to be without breaking the economy.
    private async Task<(int suggested, int min, int max)> ComputeQuoteAsync(ContractPurpose purpose, string? target, int count, CancellationToken ct)
    {
        var t = await _repo.GetRewardTuningAsync(ct);
        float baseRate = t?.BaseRewardPerCount ?? 25f;
        float diffScale = t?.DifficultyScale ?? 0.4f;
        int bandMin = t?.BandMinPercent ?? 70;
        int bandMax = t?.BandMaxPercent ?? 150;

        count = Math.Clamp(count, 1, MaxObjectiveCount);
        float difficulty = 0f;
        if (purpose == ContractPurpose.Hunt && !string.IsNullOrEmpty(target))
        {
            var monsters = await _monsters.GetAllAsync(ct);
            var m = monsters.FirstOrDefault(x => x.Key == target);
            if (m != null) difficulty = (m.MaxHealth + m.Damage * 5f) * diffScale;
        }

        int suggested = (int)MathF.Round((baseRate + difficulty) * count);
        int min = (int)MathF.Round(suggested * bandMin / 100f);
        int max = (int)MathF.Round(suggested * bandMax / 100f);
        return (suggested, min, max);
    }

    private static string AutoTitle(ContractPurpose purpose, string? target, int count) => purpose switch
    {
        ContractPurpose.Hunt when !string.IsNullOrEmpty(target) => $"Bounty: {count} {target}",
        ContractPurpose.Hunt => $"Hunt: {count} monsters",
        ContractPurpose.Survey => $"Survey: {count} site{(count > 1 ? "s" : "")}",
        ContractPurpose.Collection => $"Collection: {count} samples",
        _ => $"{purpose}: {count}",
    };

    private static string AutoDescription(ContractPurpose purpose, string? target, int count) => purpose switch
    {
        ContractPurpose.Hunt when !string.IsNullOrEmpty(target) => $"Slay {count} {target} monsters in the frontier.",
        ContractPurpose.Hunt => $"Defeat {count} monsters in the frontier.",
        ContractPurpose.Survey => $"Survey {count} site{(count > 1 ? "s" : "")} in the frontier.",
        ContractPurpose.Collection => $"Collect {count} samples in the frontier.",
        _ => $"Complete {count} objectives.",
    };

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
            ReadInt(c.ConditionsJson, "targetCount", 1), ReadInt(c.RewardJson, "currency", 0),
            ReadString(c.ConditionsJson, "target"), c.DelegationAllowed,
            ReadInt(c.RewardJson, "reputation", 0), ReadInt(c.ConditionsJson, "minReputation", 0),
            ReadInt(c.ConditionsJson, "timeLimitSeconds", 0),
            FailOnHas(c.ConditionsJson, "timeout"), FailOnHas(c.ConditionsJson, "death"),
            ReadString(c.ConditionsJson, "issuer") ?? "",
            ReadString(c.RewardJson, "itemKey") ?? "", ReadInt(c.RewardJson, "itemAmount", 0));

    // The slice's objective/reward are simple values in the Conditions/Reward JSON.
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

    // A failure condition is set if it appears in the ConditionsJson "failOn" array.
    // Absent / empty → that condition never triggers (failure is opt-in, never forced).
    private static bool FailOnHas(string json, string condition)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("failOn", out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
                foreach (var e in arr.EnumerateArray())
                    if (e.ValueKind == System.Text.Json.JsonValueKind.String && e.GetString() == condition)
                        return true;
            return false;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static string? ReadString(string json, string property)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(property, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
                ? v.GetString() : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
