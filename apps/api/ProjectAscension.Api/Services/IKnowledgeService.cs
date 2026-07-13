using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public interface IKnowledgeService
{
    Task<Result<IReadOnlyList<KnowledgeResponse>>> GetByOwnerAsync(Guid ownerActorId, CancellationToken ct = default);

    /// <summary>Sell a license for an owned, composed discovery — once per discovery
    /// (ADR 0014). Rejects (does not silently no-op) when not owned, already licensed, or
    /// the skill isn't composed yet.</summary>
    Task<Result<PlayerStateResponse>> LicenseAsync(LicenseKnowledgeRequest request, CancellationToken ct = default);
}
