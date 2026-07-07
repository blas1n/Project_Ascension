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
    private const int MaxComposeAttempts = 5; // more room to find a distinct composition now that the actor-wide Avoid set is larger
    private const int MaxLineageContext = 4;

    private readonly IDiscoveryRepository _discoveries;
    private readonly IDiscoverySkillRepository _skills;
    private readonly IKnowledgeRepository _knowledge;
    private readonly IDiscoveryLineageRepository _lineage;
    private readonly IDiscoveryTuningProvider _tuning;
    private readonly ISkillComposer _composer;
    private readonly IEffectGraphComposer _graphComposer;
    private readonly CompositionMetrics _metrics;
    private readonly ILogger<SkillCompositionService> _logger;

    public SkillCompositionService(
        IDiscoveryRepository discoveries,
        IDiscoverySkillRepository skills,
        IKnowledgeRepository knowledge,
        IDiscoveryLineageRepository lineage,
        IDiscoveryTuningProvider tuning,
        ISkillComposer composer,
        IEffectGraphComposer graphComposer,
        CompositionMetrics metrics,
        ILogger<SkillCompositionService> logger)
    {
        _discoveries = discoveries;
        _skills = skills;
        _knowledge = knowledge;
        _lineage = lineage;
        _tuning = tuning;
        _composer = composer;
        _graphComposer = graphComposer;
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

        // Claim key = play STYLE (the delivery it maps to: beam/projectile/arc/nova/burst) +
        // RARITY TIER. The style keeps the variety without fragmenting on stray movement; the
        // rarity turns "the same play, harder" into a PROGRESSION, not a duplicate — a higher
        // score yields a rarer, richer discovery (bigger budget) built on the weaker one via
        // the lineage and kept mechanically DISTINCT by the retry-on-duplicate loop (the
        // earlier rarity attempt looked like duplicates only because the tiers were identical).
        // Bounded by the ~5 rarity tiers — it climbs to Legendary and stops.
        var claimKey = $"{RegionKey(request)}:{outcome.Rarity}";
        var (discoveryId, isNew) = await CreateDiscoveryAsync(
            request.ActorId, request.RegionId, request.Type, request.Theme,
            request.ContextTags, request.PrimaryBehavior, request.Behaviors, budget.Total, parents, claimKey, ct);

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

    // Make every existing command's combo prefix-free (in CreatedAt order, so the result is
    // stable), persisting any that had to change, and return the resulting set to seed new
    // assignments. Idempotent — once settled, later passes read but don't write.
    private async Task<List<IReadOnlyList<InputToken>>> ReconcileCommandCombosAsync(CancellationToken ct)
    {
        var commands = (await _skills.GetReadyAsync(ct))
            .Where(s => string.Equals(s.Manifestation, nameof(ManifestationKind.Command), StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.CreatedAt)
            .ToList();

        var seen = new List<IReadOnlyList<InputToken>>();
        foreach (var s in commands)
        {
            var current = ComboAssigner.Parse(DeserializeTags(s.InvocationComboJson));
            var reconciled = ComboAssigner.EnsurePrefixFree(current, seen, s.DiscoveryId.ToString());
            seen.Add(reconciled);
            if (!SameCombo(current, reconciled))
            {
                s.InvocationComboJson = JsonSerializer.Serialize(reconciled.Select(t => t.ToString()).ToList());
                await _skills.UpdateAsync(s, ct);
            }
        }
        return seen;
    }

    private static bool SameCombo(IReadOnlyList<InputToken> a, IReadOnlyList<InputToken> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    // The primitive-KIND signature of an already-composed skill, matching
    // CompositionPipeline.KindSignature, so the actor-wide Avoid set forbids its effect.
    private static string? KindSignatureOf(string? primitivesJson)
    {
        if (string.IsNullOrWhiteSpace(primitivesJson)) return null;
        try
        {
            var prims = JsonSerializer.Deserialize<List<ComposedPrimitive>>(primitivesJson);
            return prims is { Count: > 0 } ? CompositionPipeline.KindSignature(prims) : null;
        }
        catch (JsonException) { return null; }
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
        // Keyed on the play STYLE — the delivery the play maps to (beam/projectile/arc/nova/
        // burst), 5 buckets. Keeps the real variety (a still charge vs. a leaping one are
        // different discoveries) yet is coarse enough that a stray jump/dodge or a rising
        // score no longer fragments one play into a stream; the composer's retry loop then
        // keeps the distinct claims mechanically unique.
        var stable = r.ContextTags
            .Where(t => !VolatileTagPrefixes.Any(p => t.StartsWith(p, StringComparison.Ordinal)))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();
        var tags = stable.Count == 0 ? "-" : string.Join(",", stable);
        return $"{r.ActorId}:{r.PrimaryBehavior}:{tags}:{DeliveryHeuristics.ForBehavior(r.Behaviors)}";
    }

    public async Task ComposePendingAsync(int batchSize, CancellationToken ct = default)
    {
        var pending = await _skills.GetPendingAsync(batchSize, ct);

        // Actor-wide dedup: every discovered skill must be mechanically distinct from every
        // skill ALREADY composed, not just from its lineage. Two plays on different behavior
        // lines could otherwise land on the same primitive-KIND set — identical effects under
        // different names, which reads as a duplicate. Seed from all Ready skills and grow the
        // set as we compose this batch (so same-pass siblings also stay distinct). Slice = one
        // actor; in the MMO this should be scoped per-discoverer (first-discoverer is personal).
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in await _skills.GetReadyAsync(ct))
        {
            var sig = KindSignatureOf(s.PrimitivesJson);
            if (sig is not null) taken.Add(sig);
        }

        // Command combos must be PREFIX-FREE across the actor so the client fires the instant a
        // combo completes. Reconcile the existing commands' combos and seed the set for new ones.
        var comboSet = await ReconcileCommandCombosAsync(ct);

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
            // (discovery.md 발견 그래프 — the graph is used, not just recorded). Avoid = the
            // actor-wide taken set so the retry steers away from every existing effect.
            request = request with
            {
                Lineage = await RetrieveLineageAsync(skill.DiscoveryId, ct),
                Avoid = taken.ToList(),
            };

            var startedAt = Stopwatch.GetTimestamp();
            var outcome = await CompositionPipeline.ForgeAsync(request, _composer, MaxComposeAttempts, ct);
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            skill.Attempts += outcome.Attempts;

            if (outcome.Forged && outcome.Skill is not null)
            {
                skill.Name = outcome.Skill.Name;
                skill.Description = outcome.Skill.Description;
                // The AI composes the delivery (guided by the prompt's behavior->delivery
                // rules); the behavior-derived heuristic is only a fallback for when it omits
                // one. The variety simulation measures whether the prompt keeps it varied.
                List<BehaviorCount> fought;
                try { fought = JsonSerializer.Deserialize<List<BehaviorCount>>(skill.BehaviorProfileJson) ?? new(); }
                catch (JsonException) { fought = new(); }
                skill.Delivery = string.IsNullOrEmpty(outcome.Skill.Delivery)
                    ? DeliveryHeuristics.ForBehavior(fought)
                    : outcome.Skill.Delivery;
                skill.PrimitivesJson = JsonSerializer.Serialize(outcome.Skill.Primitives);
                taken.Add(CompositionPipeline.KindSignature(outcome.Skill.Primitives)); // keep same-batch siblings distinct
                skill.PowerCost = outcome.LastValidation.TotalCost;
                // Deterministic, server-authoritative: an offensive skill becomes a WEAPON only
                // when magic-synthesized-from-magic (arcane/spell context, ADR 0005); a non-magic
                // offensive discovery is a cast hotkey COMMAND. Mobility → passive, etc.
                var manifestation = SkillManifest.Classify(
                    outcome.Skill, SkillManifest.IsMagicContext(DeserializeTags(skill.ContextTagsJson)));
                skill.Manifestation = manifestation.ToString();

                // A command is invoked by a button combo the rule engine assigns
                // deterministically (decoupled from the discovery behaviors — even a
                // single-behavior discovery like double jump gets one). It is made PREFIX-FREE
                // against the actor's other commands so the client can fire the instant the
                // combo completes (no disambiguation delay). Weapons fire on the attack input.
                if (manifestation == ManifestationKind.Command)
                {
                    var combo = ComboAssigner.Assign(DeserializeTags(skill.BehaviorsJson), skill.DiscoveryId.ToString());
                    combo = ComboAssigner.EnsurePrefixFree(combo, comboSet, skill.DiscoveryId.ToString());
                    comboSet.Add(combo);
                    skill.InvocationComboJson = JsonSerializer.Serialize(combo.Select(t => t.ToString()).ToList());
                }
                else
                {
                    skill.InvocationComboJson = "[]";
                }

                // Compose the effect GRAPH (ADR 0007) — the structure the runtime interpreter
                // executes. Additive during migration: if the AI produces nothing valid, the
                // skill still ships via its primitives (graph stays null, no defer).
                var graphProfile = fought.Select(b => new ProjectAscension.SkillForge.BehaviorWeight(b.Behavior, b.Count)).ToList();
                var graph = await _graphComposer.ComposeAsync(
                    new EffectGraphRequest(skill.Theme, graphProfile, new PowerBudget(skill.PowerBudget), request.Seed), ct);
                skill.EffectGraphJson = graph is null ? null : EffectGraphJson.Serialize(graph);
                if (graph is null)
                    _logger.LogWarning("No effect graph composed for discovery {DiscoveryId}; skill ships primitive-only.", skill.DiscoveryId);

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
            skill.Manifestation, contextTags, behaviors, invocationCombo, skill.Delivery, skill.EffectGraphJson);
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
