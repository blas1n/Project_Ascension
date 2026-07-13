using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface ICharacterService
{
    Task<Result<CharacterResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Mints a new Character + its Actor (atomically) and returns the actor id the
    /// client should use as its identity from now on (ADR 0014).</summary>
    Task<Result<CharacterResponse>> CreateAsync(CreateCharacterRequest request, CancellationToken ct = default);

    /// <summary>Whether an actor id is a real, known identity — used to reject an unknown
    /// actor with a clear 4xx instead of letting it fall through to a foreign-key 500.</summary>
    Task<bool> ActorExistsAsync(Guid actorId, CancellationToken ct = default);
}
