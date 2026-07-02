using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Tests;

/// <summary>Save/load of the player's progress. The service clamps and sanitises the save
/// payload (no negative currency/standing, no empty or non-positive resource stacks) and
/// round-trips resources + sold-knowledge through the profile's JSON columns.</summary>
public class PlayerProfileServiceTests
{
    private sealed class FakeProfileRepo : IPlayerProfileRepository
    {
        public PlayerProfile? Profile { get; set; }
        public int UpdateCount { get; private set; }

        public Task<PlayerProfile?> GetAsync(CancellationToken ct = default) => Task.FromResult(Profile);

        public Task UpdateAsync(PlayerProfile profile, CancellationToken ct = default)
        {
            Profile = profile;
            UpdateCount++;
            return Task.CompletedTask;
        }
    }

    private static (PlayerProfileService svc, FakeProfileRepo repo) NewService(PlayerProfile? seed = null)
    {
        var repo = new FakeProfileRepo { Profile = seed ?? new PlayerProfile { Id = 1 } };
        return (new PlayerProfileService(repo), repo);
    }

    [Fact]
    public async Task Get_NoProfile_ReturnsNotFound()
    {
        var svc = new PlayerProfileService(new FakeProfileRepo { Profile = null });

        var result = await svc.GetAsync();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Save_NoProfile_ReturnsNotFound()
    {
        var svc = new PlayerProfileService(new FakeProfileRepo { Profile = null });

        var result = await svc.SaveAsync(new SavePlayerStateRequest(10, 5,
            System.Array.Empty<ResourceCount>(), System.Array.Empty<string>()));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Save_ClampsNegativeCurrencyAndReputationToZero()
    {
        var (svc, _) = NewService();

        var result = await svc.SaveAsync(new SavePlayerStateRequest(-100, -5,
            System.Array.Empty<ResourceCount>(), System.Array.Empty<string>()));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Currency);
        Assert.Equal(0, result.Value.Reputation);
    }

    [Fact]
    public async Task Save_KeepsPositiveCurrencyAndReputation()
    {
        var (svc, _) = NewService();

        var result = await svc.SaveAsync(new SavePlayerStateRequest(250, 12,
            System.Array.Empty<ResourceCount>(), System.Array.Empty<string>()));

        Assert.Equal(250, result.Value!.Currency);
        Assert.Equal(12, result.Value.Reputation);
    }

    [Fact]
    public async Task Save_DropsEmptyKeysAndNonPositiveCounts()
    {
        var (svc, _) = NewService();

        var result = await svc.SaveAsync(new SavePlayerStateRequest(0, 0,
            new[]
            {
                new ResourceCount("hide", 3),
                new ResourceCount("", 5),      // no key → dropped
                new ResourceCount("core", 0),  // zero → dropped
                new ResourceCount("scrap", -2) // negative → dropped
            },
            System.Array.Empty<string>()));

        var resources = result.Value!.Resources;
        Assert.Single(resources);
        Assert.Equal("hide", resources[0].Key);
        Assert.Equal(3, resources[0].Count);
    }

    [Fact]
    public async Task Save_SumsDuplicateResourceKeys()
    {
        var (svc, _) = NewService();

        // A repeated key must not throw (ToDictionary would 500); the counts sum.
        var result = await svc.SaveAsync(new SavePlayerStateRequest(0, 0,
            new[] { new ResourceCount("hide", 2), new ResourceCount("hide", 3) },
            System.Array.Empty<string>()));

        Assert.True(result.IsSuccess);
        var hide = Assert.Single(result.Value!.Resources);
        Assert.Equal("hide", hide.Key);
        Assert.Equal(5, hide.Count);
    }

    [Fact]
    public async Task Save_PersistsSoldKnowledge()
    {
        var (svc, _) = NewService();

        var result = await svc.SaveAsync(new SavePlayerStateRequest(0, 0,
            System.Array.Empty<ResourceCount>(),
            new[] { "Flame Bolt", "Frost Nova" }));

        Assert.Equal(new[] { "Flame Bolt", "Frost Nova" }, result.Value!.SoldKnowledge);
    }

    [Fact]
    public async Task Save_NullCollections_TreatedAsEmpty()
    {
        var (svc, _) = NewService();

        var result = await svc.SaveAsync(new SavePlayerStateRequest(0, 0, null!, null!));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Resources);
        Assert.Empty(result.Value.SoldKnowledge);
    }

    [Fact]
    public async Task SaveThenGet_RoundTripsState()
    {
        var (svc, repo) = NewService();

        await svc.SaveAsync(new SavePlayerStateRequest(99, 4,
            new[] { new ResourceCount("hide", 2), new ResourceCount("core", 1) },
            new[] { "Thunder Lance" }));

        var loaded = await svc.GetAsync();

        Assert.Equal(1, repo.UpdateCount);
        Assert.Equal(99, loaded.Value!.Currency);
        Assert.Equal(4, loaded.Value.Reputation);
        Assert.Equal(2, loaded.Value.Resources.Length);
        Assert.Contains(loaded.Value.Resources, r => r.Key == "hide" && r.Count == 2);
        Assert.Contains(loaded.Value.Resources, r => r.Key == "core" && r.Count == 1);
        Assert.Equal(new[] { "Thunder Lance" }, loaded.Value.SoldKnowledge);
    }
}
