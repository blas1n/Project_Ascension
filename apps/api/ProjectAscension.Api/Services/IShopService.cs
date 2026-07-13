using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface IShopService
{
    Task<Result<PlayerStateResponse>> BuyAsync(BuyItemRequest request, CancellationToken ct = default);
    Task<Result<PlayerStateResponse>> SellAsync(SellItemRequest request, CancellationToken ct = default);
}
