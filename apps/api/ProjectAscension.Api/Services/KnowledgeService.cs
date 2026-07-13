using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeRepository _repo;
    private readonly IDiscoverySkillRepository _skills;
    private readonly IPlayerProfileRepository _players;
    private readonly IEconomyTuningRepository _tuning;

    public KnowledgeService(
        IKnowledgeRepository repo, IDiscoverySkillRepository skills,
        IPlayerProfileRepository players, IEconomyTuningRepository tuning)
    {
        _repo = repo;
        _skills = skills;
        _players = players;
        _tuning = tuning;
    }

    public async Task<Result<IReadOnlyList<KnowledgeResponse>>> GetByOwnerAsync(Guid ownerActorId, CancellationToken ct = default)
    {
        var items = await _repo.GetByOwnerAsync(ownerActorId, ct);
        var responses = (IReadOnlyList<KnowledgeResponse>)items
            .Select(k => new KnowledgeResponse(k.Id, k.DiscoveryId, k.OwnerActorId, k.CreatedAt))
            .ToList();
        return Result<IReadOnlyList<KnowledgeResponse>>.Ok(responses);
    }

    // Licensing sells the discovery's power as gold + standing — once per discovery (a modified
    // client could otherwise re-sell the same knowledge indefinitely). The price is derived from
    // the skill's own composed effect graph (KnowledgeValuation, shared with the client) and
    // DB-driven tuning — never from the request.
    public async Task<Result<PlayerStateResponse>> LicenseAsync(LicenseKnowledgeRequest request, CancellationToken ct = default)
    {
        if (request.ActorId == Guid.Empty || request.DiscoveryId == Guid.Empty)
            return Result<PlayerStateResponse>.Fail(Error.Invalid);

        var knowledge = await _repo.GetByDiscoveryIdAsync(request.DiscoveryId, ct);
        if (knowledge is null || knowledge.OwnerActorId != request.ActorId)
            return Result<PlayerStateResponse>.Fail(Error.NotFound); // not owned by this actor

        if (knowledge.Licensed)
            return Result<PlayerStateResponse>.Fail(Error.Conflict); // already sold — once per discovery

        var skill = await _skills.GetByDiscoveryIdAsync(request.DiscoveryId, ct);
        if (skill is null || skill.Status != DiscoveryContentStatus.Ready || string.IsNullOrEmpty(skill.EffectGraphJson))
            return Result<PlayerStateResponse>.Fail(Error.Invalid); // not composed yet — nothing to value

        var graph = EffectGraphReader.Parse(skill.EffectGraphJson);
        if (graph is null) return Result<PlayerStateResponse>.Fail(Error.Invalid);

        var profile = await _players.GetAsync(ct);
        if (profile is null) return Result<PlayerStateResponse>.Fail(Error.NotFound);

        var tuning = await _tuning.GetAsync(ct);
        int goldPerPoint = tuning?.KnowledgeGoldPerPoint ?? 6;
        int pointsPerRep = tuning?.KnowledgePointsPerRep ?? 5;

        profile.Currency += KnowledgeValuation.LicensePrice(graph, goldPerPoint);
        profile.Reputation += KnowledgeValuation.LicenseReputation(graph, pointsPerRep);
        await _players.UpdateAsync(profile, ct);

        knowledge.Licensed = true;
        knowledge.LicensedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(knowledge, ct);

        return Result<PlayerStateResponse>.Ok(PlayerProfileMapper.ToResponse(profile));
    }
}
