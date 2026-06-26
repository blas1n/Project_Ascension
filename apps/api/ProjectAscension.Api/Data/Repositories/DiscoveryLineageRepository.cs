using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class DiscoveryLineageRepository : IDiscoveryLineageRepository
{
    private readonly AppDbContext _db;
    public DiscoveryLineageRepository(AppDbContext db) => _db = db;

    public async Task AddEdgesAsync(IEnumerable<DiscoveryLineage> edges, CancellationToken ct = default)
    {
        _db.DiscoveryLineages.AddRange(edges);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DiscoveryLineage>> GetByChildAsync(Guid childDiscoveryId, CancellationToken ct = default)
        => await _db.DiscoveryLineages.AsNoTracking().Where(e => e.ChildDiscoveryId == childDiscoveryId).ToListAsync(ct);
}
