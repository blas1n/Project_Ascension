using ProjectAscension.Domain.Entities;
namespace ProjectAscension.Domain.Interfaces;

public interface IItemRepository
{
    Task<IReadOnlyList<Item>> GetByActorAsync(Guid actorId, CancellationToken ct = default);
    Task<Loadout?> GetLoadoutAsync(Guid actorId, CancellationToken ct = default);
    Task UpsertLoadoutAsync(Loadout loadout, CancellationToken ct = default);
}
