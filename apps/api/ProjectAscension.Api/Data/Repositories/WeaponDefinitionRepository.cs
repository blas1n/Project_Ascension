using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class WeaponDefinitionRepository : IWeaponDefinitionRepository
{
    private readonly AppDbContext _db;
    public WeaponDefinitionRepository(AppDbContext db) => _db = db;

    // No-tracking, read fresh: balance edits in the DB apply on the next read.
    public async Task<IReadOnlyList<WeaponDefinition>> GetAllAsync(CancellationToken ct = default)
        => await _db.WeaponDefinitions.AsNoTracking().OrderBy(w => w.Key).ToListAsync(ct);
}
