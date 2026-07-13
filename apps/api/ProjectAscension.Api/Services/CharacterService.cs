using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public class CharacterService : ICharacterService
{
    // The vertical slice's single world region (RegionConfiguration.HasData seeds this row) — a
    // fresh character starts here because the slice has exactly one frontier zone (CLAUDE.md).
    private static readonly Guid SliceRegionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ICharacterRepository _repo;
    public CharacterService(ICharacterRepository repo) => _repo = repo;

    public async Task<Result<CharacterResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var character = await _repo.GetByIdAsync(id, ct);
        if (character is null) return Result<CharacterResponse>.Fail(Error.NotFound);

        var actor = await _repo.GetActorByCharacterIdAsync(id, ct);
        if (actor is null) return Result<CharacterResponse>.Fail(Error.NotFound);

        return Result<CharacterResponse>.Ok(new CharacterResponse(
            character.Id, actor.Id, character.Name, character.CurrentRegionId, character.Status));
    }

    public async Task<Result<CharacterResponse>> CreateAsync(CreateCharacterRequest request, CancellationToken ct = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return Result<CharacterResponse>.Fail(Error.Invalid);

        var character = new Character
        {
            Id = Guid.NewGuid(),
            // No account system in the vertical slice (CLAUDE.md: out of scope) — one character
            // per install, so a fresh id here is a hook, not a real account relationship.
            AccountId = Guid.NewGuid(),
            Name = name,
            OriginRegionId = SliceRegionId,
            CurrentRegionId = SliceRegionId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
        };
        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Type = ActorType.Player,
            CharacterId = character.Id,
            CreatedAt = DateTime.UtcNow,
        };
        await _repo.CreateAsync(character, actor, ct);

        return Result<CharacterResponse>.Ok(new CharacterResponse(
            character.Id, actor.Id, character.Name, character.CurrentRegionId, character.Status));
    }

    public Task<bool> ActorExistsAsync(Guid actorId, CancellationToken ct = default)
        => _repo.ActorExistsAsync(actorId, ct);
}
