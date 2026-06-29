using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class NpcRepository : INpcRepository
{
    private readonly AppDbContext _db;
    public NpcRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<NPC>> GetAllAsync(CancellationToken ct = default)
        => await _db.NPCs.AsNoTracking().Where(n => n.Alive).OrderBy(n => n.Role).ToListAsync(ct);
}
