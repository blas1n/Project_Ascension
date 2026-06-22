using ProjectAscension.Contracts.Requests;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface ILoadoutService
{
    Task<Result<Domain.Entities.Loadout?>> GetAsync(Guid actorId, CancellationToken ct = default);
    Task<Result<Domain.Entities.Loadout>> UpdateAsync(Guid actorId, UpdateLoadoutRequest request, CancellationToken ct = default);
}
