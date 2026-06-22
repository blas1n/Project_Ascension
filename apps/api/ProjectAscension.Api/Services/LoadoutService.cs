using ProjectAscension.Contracts.Requests;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public class LoadoutService : ILoadoutService
{
    private readonly IItemRepository _repo;
    public LoadoutService(IItemRepository repo) => _repo = repo;

    public async Task<Result<Loadout?>> GetAsync(Guid actorId, CancellationToken ct = default)
    {
        var loadout = await _repo.GetLoadoutAsync(actorId, ct);
        return Result<Loadout?>.Ok(loadout);
    }

    public async Task<Result<Loadout>> UpdateAsync(Guid actorId, UpdateLoadoutRequest request, CancellationToken ct = default)
    {
        var loadout = new Loadout
        {
            ActorId = actorId,
            LeftItemId = request.LeftItemId,
            RightItemId = request.RightItemId,
            UpdatedAt = DateTime.UtcNow
        };
        await _repo.UpsertLoadoutAsync(loadout, ct);
        return Result<Loadout>.Ok(loadout);
    }
}
