using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class DiscoveryRepository : IDiscoveryRepository
{
    private readonly AppDbContext _db;
    public DiscoveryRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Discovery>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
        => await _db.Discoveries
            .Where(d => d.DiscovererActorId == actorId)
            .OrderByDescending(d => d.DiscoveredAt)
            .ToListAsync(ct);

    public async Task AddAsync(Discovery discovery, CancellationToken ct = default)
    {
        _db.Discoveries.Add(discovery);
        await _db.SaveChangesAsync(ct);
    }

    public Task<DiscoveryProgress?> GetProgressAsync(Guid actorId, Guid candidateId, CancellationToken ct = default)
        => _db.DiscoveryProgresses
            .FirstOrDefaultAsync(p => p.ActorId == actorId && p.DiscoveryCandidateId == candidateId, ct);

    public async Task UpsertProgressAsync(DiscoveryProgress progress, CancellationToken ct = default)
    {
        var existing = await GetProgressAsync(progress.ActorId, progress.DiscoveryCandidateId, ct);
        if (existing is null)
            _db.DiscoveryProgresses.Add(progress);
        else
        {
            existing.Progress = progress.Progress;
            existing.MetadataJson = progress.MetadataJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}
