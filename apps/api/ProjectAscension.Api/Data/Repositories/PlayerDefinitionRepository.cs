using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class PlayerDefinitionRepository : IPlayerDefinitionRepository
{
    private readonly AppDbContext _db;
    public PlayerDefinitionRepository(AppDbContext db) => _db = db;

    // No-tracking, read fresh: balance edits in the DB apply on the next read.
    public Task<PlayerDefinition?> GetAsync(CancellationToken ct = default)
        => _db.PlayerDefinitions.AsNoTracking().FirstOrDefaultAsync(ct);
}
