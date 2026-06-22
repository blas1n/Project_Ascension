using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface IDiscoveryService
{
    Task<Result<DiscoveryResponse>> RecordAsync(RecordDiscoveryRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DiscoveryResponse>>> GetByActorAsync(Guid actorId, CancellationToken ct = default);
}
