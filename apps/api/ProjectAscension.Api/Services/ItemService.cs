using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _repo;
    public ItemService(IItemRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<Domain.Entities.Item>>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
    {
        var items = await _repo.GetByActorAsync(actorId, ct);
        return Result<IReadOnlyList<Domain.Entities.Item>>.Ok(items);
    }
}
