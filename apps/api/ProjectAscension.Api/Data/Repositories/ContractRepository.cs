using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly AppDbContext _db;
    public ContractRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Contract>> GetByRegionAsync(Guid regionId, CancellationToken ct = default)
        => await _db.Contracts
            .Where(c => c.Status == ContractStatus.Open)
            .ToListAsync(ct);

    public Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Contracts.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Contract contract, CancellationToken ct = default)
    {
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Contract contract, CancellationToken ct = default)
    {
        _db.Contracts.Update(contract);
        await _db.SaveChangesAsync(ct);
    }

    public Task<ContractRewardTuning?> GetRewardTuningAsync(CancellationToken ct = default)
        => _db.ContractRewardTuning.AsNoTracking().FirstOrDefaultAsync(ct);
}
