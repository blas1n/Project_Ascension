using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class DiscoverySkillRepository : IDiscoverySkillRepository
{
    private readonly AppDbContext _db;
    public DiscoverySkillRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(DiscoverySkill skill, CancellationToken ct = default)
    {
        _db.DiscoverySkills.Add(skill);
        await _db.SaveChangesAsync(ct);
    }

    public Task<DiscoverySkill?> GetByDiscoveryIdAsync(Guid discoveryId, CancellationToken ct = default)
        => _db.DiscoverySkills.FirstOrDefaultAsync(s => s.DiscoveryId == discoveryId, ct);

    public async Task<IReadOnlyList<DiscoverySkill>> GetByDiscoveryIdsAsync(IEnumerable<Guid> discoveryIds, CancellationToken ct = default)
    {
        var ids = discoveryIds.ToList();
        if (ids.Count == 0) return new List<DiscoverySkill>();
        return await _db.DiscoverySkills.AsNoTracking().Where(s => ids.Contains(s.DiscoveryId)).ToListAsync(ct);
    }

    public Task<DiscoverySkill?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
        => _db.DiscoverySkills.FirstOrDefaultAsync(s => s.IdempotencyKey == key, ct);

    public async Task<IReadOnlyList<DiscoverySkill>> GetPendingAsync(int limit, CancellationToken ct = default)
        => await _db.DiscoverySkills
            .Where(s => s.Status == DiscoveryContentStatus.Pending)
            .OrderBy(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DiscoverySkill>> GetReadyAsync(CancellationToken ct = default)
        => await _db.DiscoverySkills
            .AsNoTracking()
            .Where(s => s.Status == DiscoveryContentStatus.Ready)
            .ToListAsync(ct);

    public async Task UpdateAsync(DiscoverySkill skill, CancellationToken ct = default)
    {
        _db.DiscoverySkills.Update(skill);
        await _db.SaveChangesAsync(ct);
    }
}
