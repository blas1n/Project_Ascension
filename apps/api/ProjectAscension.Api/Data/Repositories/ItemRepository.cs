using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _db;
    public ItemRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Item>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
        => await _db.Items
            .Where(i => i.OwnerActorId == actorId)
            .ToListAsync(ct);

    public Task<Loadout?> GetLoadoutAsync(Guid actorId, CancellationToken ct = default)
        => _db.Loadouts.FirstOrDefaultAsync(l => l.ActorId == actorId, ct);

    public async Task UpsertLoadoutAsync(Loadout loadout, CancellationToken ct = default)
    {
        var existing = await GetLoadoutAsync(loadout.ActorId, ct);
        if (existing is null)
            _db.Loadouts.Add(loadout);
        else
        {
            existing.LeftItemId = loadout.LeftItemId;
            existing.RightItemId = loadout.RightItemId;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}
