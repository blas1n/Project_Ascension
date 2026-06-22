using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface ICharacterService
{
    Task<Result<CharacterResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
}
