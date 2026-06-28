using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class MonsterDefinitionRepository : IMonsterDefinitionRepository
{
    private readonly AppDbContext _db;
    public MonsterDefinitionRepository(AppDbContext db) => _db = db;

    // No-tracking, read fresh: balance edits in the DB apply on the next read.
    public async Task<IReadOnlyList<MonsterDefinition>> GetAllAsync(CancellationToken ct = default)
        => await _db.MonsterDefinitions.AsNoTracking().OrderBy(m => m.Key).ToListAsync(ct);
}
