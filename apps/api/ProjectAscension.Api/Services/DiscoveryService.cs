using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public class DiscoveryService : IDiscoveryService
{
    private readonly IDiscoveryRepository _repo;
    public DiscoveryService(IDiscoveryRepository repo) => _repo = repo;

    public async Task<Result<DiscoveryResponse>> RecordAsync(RecordDiscoveryRequest request, CancellationToken ct = default)
    {
        var discovery = new Discovery
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            DiscovererActorId = request.ActorId,
            RegionId = request.RegionId,
            Title = request.Title,
            Description = request.Description,
            DiscoveredAt = DateTime.UtcNow
        };
        await _repo.AddAsync(discovery, ct);
        return Result<DiscoveryResponse>.Ok(new DiscoveryResponse(
            discovery.Id, discovery.Type, discovery.Title, discovery.Description, discovery.DiscoveredAt));
    }

    public async Task<Result<IReadOnlyList<DiscoveryResponse>>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
    {
        var discoveries = await _repo.GetByActorAsync(actorId, ct);
        var responses = (IReadOnlyList<DiscoveryResponse>)discoveries
            .Select(d => new DiscoveryResponse(d.Id, d.Type, d.Title, d.Description, d.DiscoveredAt))
            .ToList();
        return Result<IReadOnlyList<DiscoveryResponse>>.Ok(responses);
    }
}
