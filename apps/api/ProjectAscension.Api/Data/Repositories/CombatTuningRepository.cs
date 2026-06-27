using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class CombatTuningRepository : ICombatTuningRepository
{
    private readonly AppDbContext _db;
    public CombatTuningRepository(AppDbContext db) => _db = db;

    // No-tracking, read fresh: balance edits in the DB apply on the next read.
    public Task<CombatTuningSettings?> GetSettingsAsync(CancellationToken ct = default)
        => _db.CombatTuningSettings.AsNoTracking().FirstOrDefaultAsync(ct);
}
