using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly AppDbContext _db;
    public KnowledgeRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Knowledge knowledge, CancellationToken ct = default)
    {
        _db.Knowledge.Add(knowledge);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Knowledge>> GetByOwnerAsync(Guid ownerActorId, CancellationToken ct = default)
        => await _db.Knowledge
            .Where(k => k.OwnerActorId == ownerActorId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);

    public Task<Knowledge?> GetByDiscoveryIdAsync(Guid discoveryId, CancellationToken ct = default)
        => _db.Knowledge.FirstOrDefaultAsync(k => k.DiscoveryId == discoveryId, ct);

    public async Task UpdateAsync(Knowledge knowledge, CancellationToken ct = default)
    {
        _db.Knowledge.Update(knowledge);
        await _db.SaveChangesAsync(ct);
    }
}
