using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Tests;

/// <summary>The settlement growth rules — delivering resources matures infrastructure tracks
/// (2 points/unit, 10 points/level, capped at level 4) and the summed level advances the
/// civilization stage. These are server-authoritative game facts, so they are pinned here.</summary>
public class SettlementServiceTests
{
    private sealed class FakeSettlementRepo : ISettlementRepository
    {
        public Settlement? Settlement { get; set; }
        public int UpdateCount { get; private set; }

        public Task<Settlement?> GetAsync(CancellationToken ct = default) => Task.FromResult(Settlement);

        public Task UpdateAsync(Settlement settlement, CancellationToken ct = default)
        {
            Settlement = settlement;
            UpdateCount++;
            return Task.CompletedTask;
        }
    }

    private static (SettlementService svc, FakeSettlementRepo repo) NewService(Settlement? seed = null)
    {
        var repo = new FakeSettlementRepo { Settlement = seed ?? new Settlement { Id = 1, Name = "Ashford" } };
        return (new SettlementService(repo), repo);
    }

    [Fact]
    public async Task Get_NoSettlement_ReturnsNotFound()
    {
        var repo = new FakeSettlementRepo { Settlement = null };
        var svc = new SettlementService(repo);

        var result = await svc.GetAsync();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Get_MapsPointsToLevelsAndStage()
    {
        // 30 shelter pts → level 3; nothing else → total 3 → "Outpost".
        var (svc, _) = NewService(new Settlement { Id = 1, Name = "Ashford", ShelterPoints = 30 });

        var result = await svc.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Ashford", result.Value!.Name);
        Assert.Equal(3, result.Value.ShelterLevel);
        Assert.Equal(0, result.Value.MarketLevel);
        Assert.Equal(3, result.Value.TotalLevel);
        Assert.Equal("Outpost", result.Value.Stage);
    }

    [Fact]
    public async Task Deliver_NonPositiveAmount_ReturnsInvalid()
    {
        var (svc, repo) = NewService();

        var result = await svc.DeliverAsync(new DeliverResourceRequest("hide", 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, repo.UpdateCount);
    }

    [Fact]
    public async Task Deliver_NoSettlement_ReturnsNotFound()
    {
        var repo = new FakeSettlementRepo { Settlement = null };
        var svc = new SettlementService(repo);

        var result = await svc.DeliverAsync(new DeliverResourceRequest("hide", 5));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Deliver_UnknownResource_ReturnsInvalidAndDoesNotPersist()
    {
        var (svc, repo) = NewService();

        var result = await svc.DeliverAsync(new DeliverResourceRequest("gold", 5));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, repo.UpdateCount);
    }

    [Theory]
    [InlineData("hide")]
    [InlineData("feather")]
    [InlineData("core")]
    public async Task Deliver_RoutesResourceToItsTrack(string itemKey)
    {
        var (svc, repo) = NewService();

        // 5 units × 2 pts = 10 pts → exactly level 1 on the matching track only.
        var result = await svc.DeliverAsync(new DeliverResourceRequest(itemKey, 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repo.UpdateCount);
        var r = result.Value!;
        Assert.Equal(itemKey == "hide" ? 1 : 0, r.ShelterLevel);
        Assert.Equal(itemKey == "feather" ? 1 : 0, r.MarketLevel);
        Assert.Equal(itemKey == "core" ? 1 : 0, r.DefenseLevel);
        Assert.Equal(1, r.TotalLevel);
    }

    [Fact]
    public async Task Deliver_Accumulates_AcrossDeliveries()
    {
        var (svc, _) = NewService();

        await svc.DeliverAsync(new DeliverResourceRequest("hide", 3)); // 6 pts
        var result = await svc.DeliverAsync(new DeliverResourceRequest("hide", 2)); // +4 pts = 10 → level 1

        Assert.Equal(1, result.Value!.ShelterLevel);
    }

    [Fact]
    public async Task Deliver_LevelCapsAtFour()
    {
        var (svc, _) = NewService();

        // 100 units × 2 = 200 pts, far past 4×10; level must clamp at 4.
        var result = await svc.DeliverAsync(new DeliverResourceRequest("hide", 100));

        Assert.Equal(4, result.Value!.ShelterLevel);
    }

    [Theory]
    [InlineData(0, 0, 0, "Untamed")]
    [InlineData(30, 0, 0, "Outpost")]   // total 3
    [InlineData(40, 20, 0, "Settlement")] // total 6
    [InlineData(40, 40, 10, "Village")] // total 9
    [InlineData(40, 40, 40, "Town")]    // total 12 (capped 4+4+4)
    public async Task Get_StageFromTotalLevel(int shelter, int market, int defense, string expectedStage)
    {
        var (svc, _) = NewService(new Settlement
        {
            Id = 1,
            Name = "Ashford",
            ShelterPoints = shelter,
            MarketPoints = market,
            DefensePoints = defense,
        });

        var result = await svc.GetAsync();

        Assert.Equal(expectedStage, result.Value!.Stage);
    }
}
