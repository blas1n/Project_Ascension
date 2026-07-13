using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

/// <summary>City shop buy/sell — server-authoritative (ADR 0014). The client posts only the
/// INTENT (item + quantity); the price always comes from the server's own item catalog, never
/// the request body, so a modified client cannot invent a price or mint currency.</summary>
public class ShopService : IShopService
{
    private readonly IItemDefinitionRepository _items;
    private readonly IPlayerProfileRepository _players;

    public ShopService(IItemDefinitionRepository items, IPlayerProfileRepository players)
    {
        _items = items;
        _players = players;
    }

    public async Task<Result<PlayerStateResponse>> BuyAsync(BuyItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ItemKey) || request.Quantity <= 0)
            return Result<PlayerStateResponse>.Fail(Error.Invalid);

        var item = (await _items.GetAllAsync(ct)).FirstOrDefault(i => i.Key == request.ItemKey);
        if (item is null || item.BuyPrice <= 0) return Result<PlayerStateResponse>.Fail(Error.Invalid); // unknown or not buyable

        var profile = await _players.GetAsync(ct);
        if (profile is null) return Result<PlayerStateResponse>.Fail(Error.NotFound);

        int cost = item.BuyPrice * request.Quantity;
        if (profile.Currency < cost) return Result<PlayerStateResponse>.Fail(Error.Conflict); // insufficient funds

        profile.Currency -= cost;
        var resources = PlayerProfileMapper.ReadResources(profile);
        resources.TryGetValue(item.Key, out var have);
        resources[item.Key] = have + request.Quantity;
        PlayerProfileMapper.WriteResources(profile, resources);

        await _players.UpdateAsync(profile, ct);
        return Result<PlayerStateResponse>.Ok(PlayerProfileMapper.ToResponse(profile));
    }

    public async Task<Result<PlayerStateResponse>> SellAsync(SellItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ItemKey) || request.Quantity <= 0)
            return Result<PlayerStateResponse>.Fail(Error.Invalid);

        var item = (await _items.GetAllAsync(ct)).FirstOrDefault(i => i.Key == request.ItemKey);
        if (item is null || item.SellPrice <= 0) return Result<PlayerStateResponse>.Fail(Error.Invalid); // unknown or not sellable

        var profile = await _players.GetAsync(ct);
        if (profile is null) return Result<PlayerStateResponse>.Fail(Error.NotFound);

        var resources = PlayerProfileMapper.ReadResources(profile);
        resources.TryGetValue(item.Key, out var have);
        if (have < request.Quantity) return Result<PlayerStateResponse>.Fail(Error.Conflict); // insufficient stock

        int remaining = have - request.Quantity;
        if (remaining > 0) resources[item.Key] = remaining;
        else resources.Remove(item.Key); // no zero-count stacks lingering (matches PlayerProfileService's save convention)
        PlayerProfileMapper.WriteResources(profile, resources);
        profile.Currency += item.SellPrice * request.Quantity;

        await _players.UpdateAsync(profile, ct);
        return Result<PlayerStateResponse>.Ok(PlayerProfileMapper.ToResponse(profile));
    }
}
