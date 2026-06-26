using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeRepository _repo;
    public KnowledgeService(IKnowledgeRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<KnowledgeResponse>>> GetByOwnerAsync(Guid ownerActorId, CancellationToken ct = default)
    {
        var items = await _repo.GetByOwnerAsync(ownerActorId, ct);
        var responses = (IReadOnlyList<KnowledgeResponse>)items
            .Select(k => new KnowledgeResponse(k.Id, k.DiscoveryId, k.OwnerActorId, k.CreatedAt))
            .ToList();
        return Result<IReadOnlyList<KnowledgeResponse>>.Ok(responses);
    }
}
