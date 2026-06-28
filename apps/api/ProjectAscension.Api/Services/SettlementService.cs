using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface ISettlementService
{
    Task<Result<SettlementResponse>> GetAsync(CancellationToken ct = default);
    Task<Result<SettlementResponse>> DeliverAsync(DeliverResourceRequest request, CancellationToken ct = default);
}

/// <summary>The frontier outpost's growth. Delivering a resource matures the matching
/// infrastructure track (level = points / PointsPerLevel, capped at MaxLevel); the sum of
/// levels advances the settlement's civilization stage. Server-authoritative + persistent.</summary>
public class SettlementService : ISettlementService
{
    private const int PointsPerUnit = 2;   // points granted per delivered resource unit
    private const int PointsPerLevel = 10; // points to advance one infrastructure level
    private const int MaxLevel = 4;        // Absent → Early → Stable → Advanced → Complete

    private readonly ISettlementRepository _repo;
    public SettlementService(ISettlementRepository repo) => _repo = repo;

    public async Task<Result<SettlementResponse>> GetAsync(CancellationToken ct = default)
    {
        var s = await _repo.GetAsync(ct);
        return s is null
            ? Result<SettlementResponse>.Fail(Error.NotFound)
            : Result<SettlementResponse>.Ok(ToResponse(s));
    }

    public async Task<Result<SettlementResponse>> DeliverAsync(DeliverResourceRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0) return Result<SettlementResponse>.Fail(Error.Invalid);
        var s = await _repo.GetAsync(ct);
        if (s is null) return Result<SettlementResponse>.Fail(Error.NotFound);

        int points = request.Amount * PointsPerUnit;
        switch (request.ItemKey)
        {
            case "hide": s.ShelterPoints += points; break;     // shelter from hides
            case "feather": s.MarketPoints += points; break;   // market goods from feathers
            case "core": s.DefensePoints += points; break;     // defense from elite cores
            default: return Result<SettlementResponse>.Fail(Error.Invalid); // not a deliverable resource
        }
        await _repo.UpdateAsync(s, ct);
        return Result<SettlementResponse>.Ok(ToResponse(s));
    }

    private static SettlementResponse ToResponse(Domain.Entities.Settlement s)
    {
        int shelter = Level(s.ShelterPoints);
        int market = Level(s.MarketPoints);
        int defense = Level(s.DefensePoints);
        int total = shelter + market + defense;
        return new SettlementResponse(s.Name, Stage(total), shelter, market, defense, total);
    }

    private static int Level(int points) => System.Math.Min(MaxLevel, points / PointsPerLevel);

    // Civilization stage from total infrastructure maturity (settlement-evolution.md).
    private static string Stage(int totalLevel) => totalLevel switch
    {
        0 => "Untamed",
        <= 3 => "Outpost",
        <= 6 => "Settlement",
        <= 9 => "Village",
        _ => "Town",
    };
}
