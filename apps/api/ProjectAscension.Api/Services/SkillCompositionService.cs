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
    private const int MaxLineageContext = 4;

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

        var created = await CreateDiscoveryAsync(
            request.ActorId, request.RegionId, request.Type, request.Theme,
            request.ContextTags, request.PrimaryBehavior, Array.Empty<BehaviorCount>(), budget.Total,
            Array.Empty<Guid>(), request.IdempotencyKey, ct);
        return created.Id;
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
        var (discoveryId, isNew) = await CreateDiscoveryAsync(
            request.ActorId, request.RegionId, request.Type, request.Theme,
            request.ContextTags, request.PrimaryBehavior, request.Behaviors, budget.Total, parents, RegionKey(request), ct);

        // Report fired ONLY for a newly-claimed discovery. An idempotent re-hit returns the
        // existing one — reporting fired=true there made the client re-process the same
        // discovery every flush window, minting duplicate skills.
        return new EvaluateTriggerResponse(isNew, outcome.Score, isNew ? discoveryId : (Guid?)null);
    }

    private async Task<(Guid Id, bool IsNew)> CreateDiscoveryAsync(
        Guid actorId, Guid regionId, DiscoveryType type, string theme,
        IReadOnlyList<string> contextTags, string primaryBehavior, IReadOnlyList<BehaviorCount> behaviorCounts, int budget,
        IReadOnlyList<Guid> parentDiscoveryIds, string? idempotencyKey,
        CancellationToken ct)
    {
        // Idempotent: a repeated key returns the existing discovery instead of a
        // duplicate (covers client/network retries and re-evaluated regions).
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await _skills.GetByIdempotencyKeyAsync(idempotencyKey, ct);
            if (existing is not null) return (existing.DiscoveryId, false);
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
            BehaviorsJson = JsonSerializer.Serialize(behaviorCounts.Select(b => b.Behavior).ToList()),
            BehaviorProfileJson = JsonSerializer.Serialize(behaviorCounts),
            PowerBudget = budget,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow,
        };
        await _skills.AddAsync(skill, ct);

        // Record the lineage edges — permanent discovery graph (discovery.md 발견 계보).
        if (parentDiscoveryIds.Count > 0)
            await _lineage.AddEdgesAsync(
                parentDiscoveryIds.Select(p => new DiscoveryLineage { ChildDiscoveryId = discovery.Id, ParentDiscoveryId = p }), ct);

        return (discovery.Id, true);
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

    private static List<string> DeserializeTags(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch (JsonException) { return new List<string>(); }
    }

    private static IReadOnlyDictionary<string, int> ToBehaviorCounts(IReadOnlyList<BehaviorCount> behaviors)
    {
        var counts = new Dictionary<string, int>();
        foreach (var b in behaviors)
            counts[b.Behavior] = counts.GetValueOrDefault(b.Behavior) + b.Count;
        return counts;
    }

    // Tag prefixes that flavor a discovery but must NOT fragment its claim key. Transient
    // catalysts (monster:* — a rolling kill window) and the player's OWN discovered-skill
    // tags (spell:* — a feedback loop) shift every flush window; including them made each
    // window mint a fresh "first discovery", a stream of near-identical skills.
    private static readonly string[] VolatileTagPrefixes = { "monster:", "spell:" };

    private static string RegionKey(EvaluateTriggerRequest r)
    {
        // The claim key must be STABLE across a growing signature (the idempotency intent),
        // so it is built from the essential combination only — primary behavior + stable
        // context (base equipment, knowledge), excluding the volatile catalysts above —
        // PLUS a stable behavior signature so the SAME combination fought DIFFERENTLY claims
        // a new discovery (CLAUDE.md / discovery.md: behavior must matter; otherwise the
        // behavior-driven composition is unreachable — the first claim blocks the rest).
        var stable = r.ContextTags
            .Where(t => !VolatileTagPrefixes.Any(p => t.StartsWith(p, StringComparison.Ordinal)))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();
        var tags = stable.Count == 0 ? "-" : string.Join(",", stable);
        return $"{r.ActorId}:{r.PrimaryBehavior}:{tags}:{DominantAttack(r.Behaviors)}";
    }

    // The attack behaviors that define a skill's character (vs. movement, which only flavors
    // it). The dominant one decides the play style — and the delivery.
    private static readonly string[] AttackBehaviors = { "ChargedAttack", "RangedAttack", "MeleeAttack" };

    // The play style that defines the skill: the dominant ATTACK behavior. Movement
    // (jump/dodge) is deliberately excluded from the claim — varying it produced separate
    // discoveries that the composer couldn't tell apart, i.e. duplicate weapons. So the same
    // attack style claims once (whatever the footwork), and a genuinely different attack
    // style (charging vs. rapid fire vs. melee) earns a distinct discovery.
    private static string DominantAttack(IReadOnlyList<BehaviorCount> behaviors)
    {
        BehaviorCount? top = null;
        foreach (var b in behaviors)
            if (b.Count > 0 && Array.IndexOf(AttackBehaviors, b.Behavior) >= 0 && (top is null || b.Count > top.Count))
                top = b;
        return top?.Behavior ?? "-";
    }

    // The delivery is DERIVED from the dominant attack, not picked by the LLM — the model's
    // own choice collapsed to one style (every skill a hitscan beam), defeating the variety.
    // Charged → a focused beam, rapid ranged → flying projectiles, melee → a close burst, so
    // how the player fought visibly shapes how the skill manifests.
    private static string DeliveryForBehavior(IReadOnlyList<BehaviorCount> behaviors) => DominantAttack(behaviors) switch
    {
        "ChargedAttack" => "beam",
        "MeleeAttack" => "burst",
        "RangedAttack" => "projectile",
        _ => "beam",
    };

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

            // RAG: retrieve the composed lineage so the AI extends prior discoveries
            // (discovery.md 발견 그래프 — the graph is used, not just recorded).
            request = request with { Lineage = await RetrieveLineageAsync(skill.DiscoveryId, ct) };

            var startedAt = Stopwatch.GetTimestamp();
            var outcome = await CompositionPipeline.ForgeAsync(request, _composer, MaxComposeAttempts, ct);
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            skill.Attempts += outcome.Attempts;

            if (outcome.Forged && outcome.Skill is not null)
            {
                skill.Name = outcome.Skill.Name;
                skill.Description = outcome.Skill.Description;
                // Delivery is derived from how the player fought (not the LLM's pick, which
                // collapsed to a single style) so play visibly varies the manifestation.
                List<BehaviorCount> fought;
                try { fought = JsonSerializer.Deserialize<List<BehaviorCount>>(skill.BehaviorProfileJson) ?? new(); }
                catch (JsonException) { fought = new(); }
                skill.Delivery = DeliveryForBehavior(fought);
                skill.PrimitivesJson = JsonSerializer.Serialize(outcome.Skill.Primitives);
                skill.PowerCost = outcome.LastValidation.TotalCost;
                // Deterministic, server-authoritative: a synthesized-magic skill becomes
                // a weapon; everything else a command (design note / discovery.md).
                var manifestation = SkillManifest.Classify(outcome.Skill);
                skill.Manifestation = manifestation.ToString();

                // A command is invoked by a button combo the rule engine assigns
                // deterministically (decoupled from the discovery behaviors — even a
                // single-behavior discovery like double jump gets one). Weapons fire on
                // the attack input, so they need no combo.
                skill.InvocationComboJson = manifestation == ManifestationKind.Command
                    ? JsonSerializer.Serialize(ComboAssigner
                        .Assign(DeserializeTags(skill.BehaviorsJson), skill.DiscoveryId.ToString())
                        .Select(t => t.ToString()).ToList())
                    : "[]";

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

        var contextTags = DeserializeTags(skill.ContextTagsJson);
        var behaviors = DeserializeTags(skill.BehaviorsJson);
        var invocationCombo = DeserializeTags(skill.InvocationComboJson);

        return new DiscoverySkillResponse(
            skill.DiscoveryId, skill.Status, skill.Name, skill.Description, skill.PowerCost, primitives,
            skill.Manifestation, contextTags, behaviors, invocationCombo, skill.Delivery);
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

        // How the player fought (weighted) — the signal that differentiates skills built on
        // the same combination. And a seed from the discovery's identity so the composition
        // is reproducible yet unique per discovery (no two collapse to the same skill).
        List<SkillForge.BehaviorWeight> profile;
        try
        {
            profile = (JsonSerializer.Deserialize<List<BehaviorCount>>(skill.BehaviorProfileJson) ?? new List<BehaviorCount>())
                .Select(b => new SkillForge.BehaviorWeight(b.Behavior, b.Count)).ToList();
        }
        catch (JsonException)
        {
            profile = new List<SkillForge.BehaviorWeight>();
        }

        var seed = BitConverter.ToInt64(skill.DiscoveryId.ToByteArray(), 0);
        request = new CompositionRequest(
            skill.Theme, tags ?? new List<string>(), primary, new PowerBudget(skill.PowerBudget),
            Lineage: null, BehaviorProfile: profile, Seed: seed);
        return true;
    }

    private async Task<IReadOnlyList<PriorArt>> RetrieveLineageAsync(Guid discoveryId, CancellationToken ct)
    {
        // Pull the immediate composed ancestors — the strongest, bounded context for
        // the composer to build on (RAG over the discovery graph).
        var edges = await _lineage.GetByChildAsync(discoveryId, ct);
        if (edges.Count == 0) return Array.Empty<PriorArt>();

        var parents = await _skills.GetByDiscoveryIdsAsync(edges.Select(e => e.ParentDiscoveryId), ct);

        var priorArt = new List<PriorArt>();
        foreach (var s in parents)
        {
            // Only Ready ancestors carry composed content to build on.
            if (s.Status != DiscoveryContentStatus.Ready || s.Name is null || s.PrimitivesJson is null) continue;

            List<ComposedPrimitive>? prims;
            try { prims = JsonSerializer.Deserialize<List<ComposedPrimitive>>(s.PrimitivesJson); }
            catch (JsonException) { continue; }

            priorArt.Add(new PriorArt(s.Name, s.Description ?? string.Empty, prims ?? new List<ComposedPrimitive>()));
            if (priorArt.Count >= MaxLineageContext) break;
        }
        return priorArt;
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
