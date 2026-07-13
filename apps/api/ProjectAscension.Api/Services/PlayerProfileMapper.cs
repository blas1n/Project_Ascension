using System.Text.Json;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Api.Services;

/// <summary>Shared (de)serialization for the player profile's JSON columns + the response
/// projection. Every economy endpoint (contracts, shop, knowledge) returns this SAME
/// authoritative shape after mutating the profile (ADR 0014) — one place owns the wire format
/// so they can't drift.</summary>
public static class PlayerProfileMapper
{
    public static PlayerStateResponse ToResponse(PlayerProfile p)
    {
        var resources = ReadResources(p);
        var sold = Deserialize<string[]>(p.SoldKnowledgeJson) ?? Array.Empty<string>();
        return new PlayerStateResponse(
            p.Currency, p.Reputation,
            resources.Select(kv => new ResourceCount(kv.Key, kv.Value)).ToArray(),
            sold);
    }

    public static Dictionary<string, int> ReadResources(PlayerProfile p)
        => Deserialize<Dictionary<string, int>>(p.ResourcesJson) ?? new();

    public static void WriteResources(PlayerProfile p, Dictionary<string, int> resources)
        => p.ResourcesJson = JsonSerializer.Serialize(resources);

    private static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json); }
        catch (JsonException) { return default; }
    }
}
