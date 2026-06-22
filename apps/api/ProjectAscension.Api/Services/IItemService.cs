using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface IItemService
{
    Task<Result<IReadOnlyList<Domain.Entities.Item>>> GetByActorAsync(Guid actorId, CancellationToken ct = default);
}
