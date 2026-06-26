using System.Diagnostics;
using System.Text.Json;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Services;

public class SkillCompositionService : ISkillCompositionService
{
    private const int MaxComposeAttempts = 3;

    private readonly IDiscoveryRepository _discoveries;
    private readonly IDiscoverySkillRepository _skills;
    private readonly IKnowledgeRepository _knowledge;
    private readonly ISkillComposer _composer;
    private readonly CompositionMetrics _metrics;
    private readonly ILogger<SkillCompositionService> _logger;

    public SkillCompositionService(
        IDiscoveryRepository discoveries,
        IDiscoverySkillRepository skills,
        IKnowledgeRepository knowledge,
        ISkillComposer composer,
        CompositionMetrics metrics,
        ILogger<SkillCompositionService> logger)
    {
        _discoveries = discoveries;
        _skills = skills;
        _knowledge = knowledge;
        _composer = composer;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Guid> TriggerAsync(TriggerDiscoveryRequest request, CancellationToken ct = default)
    {
        // Idempotent retry: a trigger repeating a key returns the existing discovery
        // instead of creating a duplicate (covers client/network retries).
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existing = await _skills.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
            if (existing is not null) return existing.DiscoveryId;
        }

        // Rule engine fixes the fact instantly (ADR 0002): who/where/when, deterministic.
        var discovery = new Discovery
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            DiscovererActorId = request.ActorId,
            RegionId = request.RegionId,
            Title = string.IsNullOrWhiteSpace(request.Theme) ? "Discovery" : request.Theme,
            Description = string.Empty,
            DiscoveredAt = DateTime.UtcNow,
        };
        await _discoveries.AddAsync(discovery, ct);

        // The first discoverer is the first owner — knowledge as an asset
        // (discovery.md 소유권 생성). Architecture hook; the economy is out of scope.
        await _knowledge.AddAsync(new Knowledge
        {
            Id = Guid.NewGuid(),
            DiscoveryId = discovery.Id,
            OwnerActorId = request.ActorId,
            CreatedAt = DateTime.UtcNow,
        }, ct);

        // The rule engine owns the power budget — derived from rarity, never sent by
        // the client (ADR 0002: numbers are server-authoritative).
        var rarity = Enum.TryParse<Rarity>(request.Rarity, ignoreCase: true, out var parsed) ? parsed : Rarity.Common;
        var budget = BudgetRules.Derive(rarity);

        // Content starts Pending; the AI fills it asynchronously.
        var skill = new DiscoverySkill
        {
            Id = Guid.NewGuid(),
            DiscoveryId = discovery.Id,
            Status = DiscoveryContentStatus.Pending,
            Theme = request.Theme,
            ContextTagsJson = JsonSerializer.Serialize(request.ContextTags),
            PrimaryBehavior = request.PrimaryBehavior,
            PowerBudget = budget.Total,
            IdempotencyKey = request.IdempotencyKey,
            CreatedAt = DateTime.UtcNow,
        };
        await _skills.AddAsync(skill, ct);
        return discovery.Id;
    }

    public async Task ComposePendingAsync(int batchSize, CancellationToken ct = default)
    {
        var pending = await _skills.GetPendingAsync(batchSize, ct);
        foreach (var skill in pending)
        {
            if (!TryBuildRequest(skill, out var request))
            {
                // Malformed seed — defer (count the attempt). No fallback skill.
                skill.Attempts++;
                await _skills.UpdateAsync(skill, ct);
                continue;
            }

            var startedAt = Stopwatch.GetTimestamp();
            var outcome = await CompositionPipeline.ForgeAsync(request, _composer, MaxComposeAttempts, ct);
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            skill.Attempts += outcome.Attempts;

            if (outcome.Forged && outcome.Skill is not null)
            {
                skill.Name = outcome.Skill.Name;
                skill.Description = outcome.Skill.Description;
                skill.PrimitivesJson = JsonSerializer.Serialize(outcome.Skill.Primitives);
                skill.PowerCost = outcome.LastValidation.TotalCost;
                skill.Status = DiscoveryContentStatus.Ready;
                skill.ComposedAt = DateTime.UtcNow;

                _metrics.Completed(outcome.Attempts, elapsedMs);
                _logger.LogInformation(
                    "Composed \"{Name}\" for discovery {DiscoveryId} in {Attempts} attempt(s), {ElapsedMs:F0}ms (cost {Cost}/{Budget}).",
                    skill.Name, skill.DiscoveryId, outcome.Attempts, elapsedMs, skill.PowerCost, skill.PowerBudget);
            }
            else
            {
                // Leave Pending — retried on a later pass (defer, no fallback).
                _metrics.Deferred(outcome.Attempts, elapsedMs);
                _logger.LogWarning(
                    "Deferred composition for discovery {DiscoveryId} after {Attempts} attempt(s): {Error}.",
                    skill.DiscoveryId, outcome.Attempts, outcome.LastValidation.Error);
            }

            await _skills.UpdateAsync(skill, ct);
        }
    }

    public async Task<DiscoverySkillResponse?> GetByDiscoveryAsync(Guid discoveryId, CancellationToken ct = default)
    {
        var skill = await _skills.GetByDiscoveryIdAsync(discoveryId, ct);
        if (skill is null) return null;

        var primitives = skill.PrimitivesJson is null
            ? new List<string>()
            : DescribePrimitives(skill.PrimitivesJson);

        return new DiscoverySkillResponse(
            skill.DiscoveryId, skill.Status, skill.Name, skill.Description, skill.PowerCost, primitives);
    }

    private static bool TryBuildRequest(DiscoverySkill skill, out CompositionRequest request)
    {
        request = default!;
        if (!Enum.TryParse<PrimitiveKind>(skill.PrimaryBehavior, ignoreCase: true, out var primary))
            return false;

        List<string>? tags;
        try
        {
            tags = JsonSerializer.Deserialize<List<string>>(skill.ContextTagsJson);
        }
        catch (JsonException)
        {
            return false;
        }

        request = new CompositionRequest(skill.Theme, tags ?? new List<string>(), primary, new PowerBudget(skill.PowerBudget));
        return true;
    }

    private static IReadOnlyList<string> DescribePrimitives(string json)
    {
        try
        {
            var primitives = JsonSerializer.Deserialize<List<ComposedPrimitive>>(json) ?? new List<ComposedPrimitive>();
            return primitives.Select(p => $"{p.Kind} x{p.Magnitude}").ToList();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
