using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface IKnowledgeService
{
    Task<Result<IReadOnlyList<KnowledgeResponse>>> GetByOwnerAsync(Guid ownerActorId, CancellationToken ct = default);
}
