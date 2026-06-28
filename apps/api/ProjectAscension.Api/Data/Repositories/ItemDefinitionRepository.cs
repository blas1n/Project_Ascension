using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class ItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly AppDbContext _db;
    public ItemDefinitionRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ItemDefinition>> GetAllAsync(CancellationToken ct = default)
        => await _db.ItemDefinitions.AsNoTracking().OrderBy(i => i.Key).ToListAsync(ct);
}
