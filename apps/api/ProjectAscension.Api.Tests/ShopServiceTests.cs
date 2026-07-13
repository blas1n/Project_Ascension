using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Tests;

/// <summary>City shop buy/sell (ADR 0014): the price always comes from the server's item
/// catalog, never the request — an unknown/non-tradeable item is rejected, and funds/stock
/// are rejected outright (not silently clamped) when insufficient.</summary>
public class ShopServiceTests
{
    private sealed class FakeItemRepo : IItemDefinitionRepository
    {
        public List<ItemDefinition> Items { get; } = new();
        public Task<IReadOnlyList<ItemDefinition>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ItemDefinition>>(Items.ToList());
    }

    private sealed class FakePlayerProfileRepo : IPlayerProfileRepository
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

    private static (ShopService svc, FakeItemRepo items, FakePlayerProfileRepo players) NewService(
        int currency = 100, Dictionary<string, int>? resources = null)
    {
        var items = new FakeItemRepo();
        items.Items.Add(new ItemDefinition { Key = "hide", DisplayName = "Hide", SellPrice = 5, BuyPrice = 8 });
        items.Items.Add(new ItemDefinition { Key = "map", DisplayName = "Map", SellPrice = 0, BuyPrice = 0 }); // untradeable

        var resourcesJson = resources is null ? "{}" : System.Text.Json.JsonSerializer.Serialize(resources);
        var players = new FakePlayerProfileRepo { Profile = new PlayerProfile { Id = 1, Currency = currency, ResourcesJson = resourcesJson } };
        return (new ShopService(items, players), items, players);
    }

    // --- Buy ----------------------------------------------------------------

    [Fact]
    public async Task Buy_UnknownItem_ReturnsInvalid()
    {
        var (svc, _, _) = NewService();

        var result = await svc.BuyAsync(new BuyItemRequest("ghost", 1));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Buy_NotBuyable_ReturnsInvalid()
    {
        var (svc, _, _) = NewService();

        var result = await svc.BuyAsync(new BuyItemRequest("map", 1));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Buy_InsufficientFunds_ReturnsConflict_AndChangesNothing()
    {
        var (svc, _, players) = NewService(currency: 5); // hide costs 8

        var result = await svc.BuyAsync(new BuyItemRequest("hide", 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(5, players.Profile!.Currency);
        Assert.Equal(0, players.UpdateCount);
    }

    [Fact]
    public async Task Buy_Affordable_ChargesServerPriceAndAddsResource()
    {
        var (svc, _, players) = NewService(currency: 100);

        var result = await svc.BuyAsync(new BuyItemRequest("hide", 3)); // 3 × 8 = 24

        Assert.True(result.IsSuccess);
        Assert.Equal(76, players.Profile!.Currency);
        Assert.Equal(76, result.Value!.Currency);
        var hide = Assert.Single(result.Value.Resources, r => r.Key == "hide");
        Assert.Equal(3, hide.Count);
    }

    [Fact]
    public async Task Buy_IgnoresAnyPriceInTheRequest_OnlyIntentMatters()
    {
        // BuyItemRequest doesn't even carry a price field — this pins that the server-side
        // price is always item.BuyPrice regardless of quantity chosen, not something the
        // client could smuggle in.
        var (svc, _, players) = NewService(currency: 100);

        await svc.BuyAsync(new BuyItemRequest("hide", 1));

        Assert.Equal(92, players.Profile!.Currency); // exactly BuyPrice (8), not something lower
    }

    // --- Sell -----------------------------------------------------------------

    [Fact]
    public async Task Sell_UnknownItem_ReturnsInvalid()
    {
        var (svc, _, _) = NewService();

        var result = await svc.SellAsync(new SellItemRequest("ghost", 1));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Sell_NotSellable_ReturnsInvalid()
    {
        var (svc, _, _) = NewService(resources: new() { ["map"] = 1 });

        var result = await svc.SellAsync(new SellItemRequest("map", 1));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Sell_InsufficientStock_ReturnsConflict_AndChangesNothing()
    {
        var (svc, _, players) = NewService(currency: 10, resources: new() { ["hide"] = 1 });

        var result = await svc.SellAsync(new SellItemRequest("hide", 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(10, players.Profile!.Currency);
        Assert.Equal(0, players.UpdateCount);
    }

    [Fact]
    public async Task Sell_SufficientStock_PaysServerPriceAndRemovesStock()
    {
        var (svc, _, players) = NewService(currency: 0, resources: new() { ["hide"] = 3 });

        var result = await svc.SellAsync(new SellItemRequest("hide", 2)); // 2 × 5 = 10

        Assert.True(result.IsSuccess);
        Assert.Equal(10, players.Profile!.Currency);
        var hide = Assert.Single(result.Value!.Resources, r => r.Key == "hide");
        Assert.Equal(1, hide.Count);
    }

    [Fact]
    public async Task Sell_ExactlyAllStock_DropsTheZeroEntry()
    {
        var (svc, _, players) = NewService(currency: 0, resources: new() { ["hide"] = 2 });

        var result = await svc.SellAsync(new SellItemRequest("hide", 2));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Resources); // no lingering zero-count stack
    }
}
