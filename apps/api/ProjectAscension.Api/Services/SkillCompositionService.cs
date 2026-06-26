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
    private readonly IDiscoveryLineageRepository _lineage;
    private readonly IDiscoveryTuningProvider _tuning;
    private readonly ISkillComposer _composer;
    private readonly CompositionMetrics _metrics;
    private readonly ILogger<SkillCompositionService> _logger;

    public SkillCompositionService(
        IDiscoveryRepository discoveries,
        IDiscoverySkillRepository skills,
        IKnowledgeRepository knowledge,
        IDiscoveryLineageRepository lineage,
        IDiscoveryTuningProvider tuning,
        ISkillComposer composer,
        CompositionMetrics metrics,
        ILogger<SkillCompositionService> logger)
    {
        _discoveries = discoveries;
        _skills = skills;
        _knowledge = knowledge;
        _lineage = lineage;
        _tuning = tuning;
        _composer = composer;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Guid> TriggerAsync(TriggerDiscoveryRequest request, CancellationToken ct = default)
    {
        // Manual path: rarity is supplied; the rule engine maps it to a budget on the
        // tuned curve (numbers stay server-authoritative, ADR 0002).
        var tuning = await _tuning.GetAsync(ct);
        var rarity = Enum.TryParse<Rarity>(request.Rarity, ignoreCase: true, out var parsed) ? parsed : Rarity.Common;
        var budget = BudgetRules.FromRarity(rarity, tuning);

        return await CreateDiscoveryAsync(
            request.ActorId, request.RegionId, request.Type, request.Theme,
            request.ContextTags, request.PrimaryBehavior, budget.Total, Array.Empty<Guid>(), request.IdempotencyKey, ct);
    }

    public async Task<EvaluateTriggerResponse> EvaluateAndTriggerAsync(EvaluateTriggerRequest request, CancellationToken ct = default)
    {
        // The trigger is a function, not a catalog (ADR 0002 core 4): the rule engine
        // scores the actual behavior combination against the runtime tuning and fires
        // only when it crosses the significance threshold.
        var tuning = await _tuning.GetAsync(ct);

        // Prior owned discoveries in this space become the new discovery's parents and
        // deepen it (discovery.md 발견 그래프: "발견은 다음 발견의 시작").
        var parents = await ComputeParentsAsync(request.ActorId, request.ContextTags, request.PrimaryBehavior, ct);
        var signature = new BehaviorSignature(
            ToBehaviorCounts(request.Behaviors), request.ContextTags, parents.Count, request.Persistence);
        var outcome = TriggerEvaluator.Evaluate(signature, tuning);
        if (!outcome.Fires)
            return new EvaluateTriggerResponse(false, outcome.Score, null);

        // Budget scales with the score, so a stronger pattern yields a richer skill.
        var budget = BudgetRules.FromScore(outcome.Score, tuning);

        // Claim the behavior region once (first-discoverer) via an idempotency key,
        // so repeated evaluations of a still-growing signature don't re-fire it.
        var discoveryId = await CreateDiscoveryAsync(
            request.ActorId, request.RegionId, request.Type, request.Theme,
            request.ContextTags, request.PrimaryBehavior, budget.Total, parents, RegionKey(request), ct);

        return new EvaluateTriggerResponse(true, outcome.Score, discoveryId);
    }

    private async Task<Guid> CreateDiscoveryAsync(
        Guid actorId, Guid regionId, DiscoveryType type, string theme,
        IReadOnlyList<string> contextTags, string primaryBehavior, int budget,
        IReadOnlyList<Guid> parentDiscoveryIds, string? idempotencyKey,
        CancellationToken ct)
    {
        // Idempotent: a repeated key returns the existing discovery instead of a
        // duplicate (covers client/network retries and re-evaluated regions).
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await _skills.GetByIdempotencyKeyAsync(idempotencyKey, ct);
            if (existing is not null) return existing.DiscoveryId;
        }

        // Rule engine fixes the fact instantly (ADR 0002): who/where/when, deterministic.
        var discovery = new Discovery
        {
            Id = Guid.NewGuid(),
            Type = type,
            DiscovererActorId = actorId,
            RegionId = regionId,
            Title = string.IsNullOrWhiteSpace(theme) ? "Discovery" : theme,
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
            OwnerActorId = actorId,
            CreatedAt = DateTime.UtcNow,
        }, ct);

        // Content starts Pending; the AI fills it asynchronously.
        var skill = new DiscoverySkill
        {
            Id = Guid.NewGuid(),
            DiscoveryId = discovery.Id,
            Status = DiscoveryContentStatus.Pending,
            Theme = theme,
            ContextTagsJson = JsonSerializer.Serialize(contextTags),
            PrimaryBehavior = primaryBehavior,
            PowerBudget = budget,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow,
        };
        await _skills.AddAsync(skill, ct);

        // Record the lineage edges — permanent discovery graph (discovery.md 발견 계보).
        if (parentDiscoveryIds.Count > 0)
            await _lineage.AddEdgesAsync(
                parentDiscoveryIds.Select(p => new DiscoveryLineage { ChildDiscoveryId = discovery.Id, ParentDiscoveryId = p }), ct);

        return discovery.Id;
    }

    private async Task<IReadOnlyList<Guid>> ComputeParentsAsync(
        Guid actorId, IReadOnlyList<string> contextTags, string primaryBehavior, CancellationToken ct)
    {
        var owned = await _knowledge.GetByOwnerAsync(actorId, ct);
        if (owned.Count == 0) return Array.Empty<Guid>();

        var ownedSkills = await _skills.GetByDiscoveryIdsAsync(owned.Select(k => k.DiscoveryId), ct);
        var tagSet = new HashSet<string>(contextTags, StringComparer.OrdinalIgnoreCase);

        var parents = new List<Guid>();
        foreach (var s in ownedSkills)
        {
            // A prior discovery is a parent if it sits in the same knowledge space
            // (shares a context tag) or the same skill line (same primary behavior).
            bool sameBehavior = string.Equals(s.PrimaryBehavior, primaryBehavior, StringComparison.OrdinalIgnoreCase);
            bool sharedTag = false;
            try
            {
                var tags = JsonSerializer.Deserialize<List<string>>(s.ContextTagsJson) ?? new List<string>();
                sharedTag = tags.Any(tagSet.Contains);
            }
            catch (JsonException) { }

            if (sameBehavior || sharedTag) parents.Add(s.DiscoveryId);
        }
        return parents;
    }

    public async Task<DiscoveryLineageResponse> GetLineageAsync(Guid discoveryId, CancellationToken ct = default)
    {
        // Walk parent edges upward (graph, nearest-first), guarding against cycles.
        var ancestors = new List<Guid>();
        var visited = new HashSet<Guid> { discoveryId };
        var frontier = new Queue<Guid>();
        frontier.Enqueue(discoveryId);

        int guard = 0;
        while (frontier.Count > 0 && guard++ < 256)
        {
            var edges = await _lineage.GetByChildAsync(frontier.Dequeue(), ct);
            foreach (var e in edges)
            {
                if (!visited.Add(e.ParentDiscoveryId)) continue;
                ancestors.Add(e.ParentDiscoveryId);
                frontier.Enqueue(e.ParentDiscoveryId);
            }
        }

        if (ancestors.Count == 0)
            return new DiscoveryLineageResponse(discoveryId, new List<LineageEntry>());

        var skills = await _skills.GetByDiscoveryIdsAsync(ancestors, ct);
        var nameById = skills.ToDictionary(s => s.DiscoveryId, s => s.Name ?? s.Theme);
        var entries = ancestors
            .Select(a => new LineageEntry(a, nameById.TryGetValue(a, out var n) ? n : string.Empty))
            .ToList();
        return new DiscoveryLineageResponse(discoveryId, entries);
    }

    private static IReadOnlyDictionary<string, int> ToBehaviorCounts(IReadOnlyList<BehaviorCount> behaviors)
    {
        var counts = new Dictionary<string, int>();
        foreach (var b in behaviors)
            counts[b.Behavior] = counts.GetValueOrDefault(b.Behavior) + b.Count;
        return counts;
    }

    private static string RegionKey(EvaluateTriggerRequest r)
    {
        var tags = r.ContextTags.Count == 0
            ? "-"
            : string.Join(",", r.ContextTags.OrderBy(t => t, StringComparer.Ordinal));
        return $"{r.ActorId}:{r.PrimaryBehavior}:{tags}";
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
            return primitives.Select(p =>
            {
                var extra = (p.Range > 0 ? $" r{p.Range}" : string.Empty) + (p.Duration > 0 ? $" d{p.Duration}" : string.Empty);
                return $"{p.Kind} x{p.Magnitude}{extra}";
            }).ToList();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
