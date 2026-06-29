using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class PlayerProfileRepository : IPlayerProfileRepository
{
    private readonly AppDbContext _db;
    public PlayerProfileRepository(AppDbContext db) => _db = db;

    public Task<PlayerProfile?> GetAsync(CancellationToken ct = default)
        => _db.PlayerProfiles.FirstOrDefaultAsync(ct);

    public async Task UpdateAsync(PlayerProfile profile, CancellationToken ct = default)
    {
        _db.PlayerProfiles.Update(profile);
        await _db.SaveChangesAsync(ct);
    }
}
