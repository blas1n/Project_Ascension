using Microsoft.EntityFrameworkCore;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Data.Repositories;

public class CharacterRepository : ICharacterRepository
{
    private readonly AppDbContext _db;
    public CharacterRepository(AppDbContext db) => _db = db;

    public Task<Character?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Characters.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Actor?> GetActorByCharacterIdAsync(Guid characterId, CancellationToken ct = default)
        => _db.Actors.FirstOrDefaultAsync(a => a.CharacterId == characterId, ct);
}
