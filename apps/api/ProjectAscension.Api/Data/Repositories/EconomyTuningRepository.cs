using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class EconomyTuningRepository : IEconomyTuningRepository
{
    private readonly AppDbContext _db;
    public EconomyTuningRepository(AppDbContext db) => _db = db;

    public Task<EconomyTuning?> GetAsync(CancellationToken ct = default)
        => _db.EconomyTuning.AsNoTracking().FirstOrDefaultAsync(ct);
}
