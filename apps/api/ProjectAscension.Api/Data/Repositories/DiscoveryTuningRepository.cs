using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class DiscoveryTuningRepository : IDiscoveryTuningRepository
{
    private readonly AppDbContext _db;
    public DiscoveryTuningRepository(AppDbContext db) => _db = db;

    // No-tracking, read fresh: balance edits in the DB apply on the next evaluation.
    public Task<DiscoveryTuningSettings?> GetSettingsAsync(CancellationToken ct = default)
        => _db.DiscoveryTuningSettings.AsNoTracking().FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BehaviorWeight>> GetBehaviorWeightsAsync(CancellationToken ct = default)
        => await _db.BehaviorWeights.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<FactorWeight>> GetFactorWeightsAsync(CancellationToken ct = default)
        => await _db.FactorWeights.AsNoTracking().ToListAsync(ct);
}
