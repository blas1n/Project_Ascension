using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public class CharacterService : ICharacterService
{
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
}
