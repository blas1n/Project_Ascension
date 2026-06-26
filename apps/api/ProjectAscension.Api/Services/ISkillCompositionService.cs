using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;

namespace ProjectAscension.Api.Services;

/// <summary>
/// Fact/content separation for discoveries (ADR 0002). <see cref="TriggerAsync"/>
/// fixes the fact instantly and queues the content as Pending;
/// <see cref="ComposePendingAsync"/> (driven by a background worker) composes the
/// AI skill and freezes it to Ready.
/// </summary>
public interface ISkillCompositionService
{
    Task<Guid> TriggerAsync(TriggerDiscoveryRequest request, CancellationToken ct = default);
    Task<EvaluateTriggerResponse> EvaluateAndTriggerAsync(EvaluateTriggerRequest request, CancellationToken ct = default);
    Task<DiscoverySkillResponse?> GetByDiscoveryAsync(Guid discoveryId, CancellationToken ct = default);
    Task<DiscoveryLineageResponse> GetLineageAsync(Guid discoveryId, CancellationToken ct = default);
    Task ComposePendingAsync(int batchSize, CancellationToken ct = default);
}
