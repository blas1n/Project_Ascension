using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class SettlementRepository : ISettlementRepository
{
    private readonly AppDbContext _db;
    public SettlementRepository(AppDbContext db) => _db = db;

    public Task<Settlement?> GetAsync(CancellationToken ct = default)
        => _db.Settlements.FirstOrDefaultAsync(ct);

    public async Task UpdateAsync(Settlement settlement, CancellationToken ct = default)
    {
        _db.Settlements.Update(settlement);
        await _db.SaveChangesAsync(ct);
    }
}
