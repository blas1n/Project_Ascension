using System.Text.Json;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface IPlayerProfileService
{
    Task<Result<PlayerStateResponse>> GetAsync(CancellationToken ct = default);
    Task<Result<PlayerStateResponse>> SaveAsync(SavePlayerStateRequest request, CancellationToken ct = default);
}

/// <summary>Loads and persists the player's progress. Resources/sold-knowledge are stored
/// as JSON on the profile; the service converts to/from the typed save shape.</summary>
public class PlayerProfileService : IPlayerProfileService
{
    private readonly IPlayerProfileRepository _repo;
    public PlayerProfileService(IPlayerProfileRepository repo) => _repo = repo;

    public async Task<Result<PlayerStateResponse>> GetAsync(CancellationToken ct = default)
    {
        var p = await _repo.GetAsync(ct);
        return p is null
            ? Result<PlayerStateResponse>.Fail(Error.NotFound)
            : Result<PlayerStateResponse>.Ok(ToResponse(p));
    }

    public async Task<Result<PlayerStateResponse>> SaveAsync(SavePlayerStateRequest request, CancellationToken ct = default)
    {
        var p = await _repo.GetAsync(ct);
        if (p is null) return Result<PlayerStateResponse>.Fail(Error.NotFound);

        p.Currency = System.Math.Max(0, request.Currency);
        p.Reputation = System.Math.Max(0, request.Reputation);
        // Sum duplicate keys instead of ToDictionary (which throws on a repeated key → 500).
        var resources = (request.Resources ?? System.Array.Empty<ResourceCount>())
            .Where(r => !string.IsNullOrEmpty(r.Key) && r.Count > 0)
            .GroupBy(r => r.Key)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Count));
        p.ResourcesJson = JsonSerializer.Serialize(resources);
        p.SoldKnowledgeJson = JsonSerializer.Serialize(request.SoldKnowledge ?? System.Array.Empty<string>());

        await _repo.UpdateAsync(p, ct);
        return Result<PlayerStateResponse>.Ok(ToResponse(p));
    }

    private static PlayerStateResponse ToResponse(Domain.Entities.PlayerProfile p)
    {
        var resources = Deserialize<Dictionary<string, int>>(p.ResourcesJson) ?? new();
        var sold = Deserialize<string[]>(p.SoldKnowledgeJson) ?? System.Array.Empty<string>();
        return new PlayerStateResponse(
            p.Currency, p.Reputation,
            resources.Select(kv => new ResourceCount(kv.Key, kv.Value)).ToArray(),
            sold);
    }

    private static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json); }
        catch (JsonException) { return default; }
    }
}
