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
    private const int MaxLineageContext = 4;

    private readonly IDiscoveryRepository _discoveries;
    private readonly IDiscoverySkillRepository _skills;
    private readonly IKnowledgeRepository _knowledge;
    private readonly IDiscoveryLineageRepository _lineage;
    private readonly IDiscoveryTuningProvider _tuning;
    private readonly IEffectGraphComposer _graphComposer;
    private readonly CompositionMetrics _metrics;
    private readonly ILogger<SkillCompositionService> _logger;

    public SkillCompositionService(
        IDiscoveryRepository discoveries,
        IDiscoverySkillRepository skills,
        IKnowledgeRepository knowledge,
        IDiscoveryLineageRepository lineage,
        IDiscoveryTuningProvider tuning,
        IEffectGraphComposer graphComposer,
        CompositionMetrics metrics,
        ILogger<SkillCompositionService> logger)
    {
        _discoveries = discoveries;
        _skills = skills;
        _knowledge = knowledge;
        _lineage = lineage;
        _tuning = tuning;
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

    // The composed ancestors a discovery builds on — Ready skills with a graph, nearest first
    // (RAG over the discovery graph, ADR 0007). The graph composer evolves their theme/structure.
    private async Task<IReadOnlyList<SkillLineage>> RetrieveGraphLineageAsync(Guid discoveryId, CancellationToken ct)
    {
        var edges = await _lineage.GetByChildAsync(discoveryId, ct);
        if (edges.Count == 0) return Array.Empty<SkillLineage>();

        var parents = await _skills.GetByDiscoveryIdsAsync(edges.Select(e => e.ParentDiscoveryId), ct);
        var lineage = new List<SkillLineage>();
        foreach (var s in parents)
        {
            if (s.Status != DiscoveryContentStatus.Ready || s.Name is null || string.IsNullOrEmpty(s.EffectGraphJson)) continue;
            lineage.Add(new SkillLineage(s.Name, s.Description ?? string.Empty, s.EffectGraphJson));
            if (lineage.Count >= MaxLineageContext) break;
        }
        return lineage;
    }

    // The delivery SHAPE ("projectile"/"beam"/"burst"/"nova") of a composed graph — its first
    // Emit. Null when the skill emits nothing (movement/ward), so the DTO falls back to a heuristic.
    private static string? FirstDelivery(EffectNode node)
    {
        switch (node)
        {
            case Emit e: return e.Delivery.ToString().ToLowerInvariant();
            case Trigger t: return FirstDelivery(t.Child);
            case Sequence s:
                foreach (var step in s.Steps)
                {
                    var d = FirstDelivery(step);
                    if (d is not null) return d;
                }
                return null;
            default: return null;
        }
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
        // Actor-wide dedup on the GRAPH signature (its canonical serialization) — every new skill
        // must be structurally distinct from every one already composed. Seeded from the Ready
        // skills' graphs and grown as this batch composes (so same-pass siblings stay distinct too).
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in await _skills.GetReadyAsync(ct))
            if (!string.IsNullOrEmpty(s.EffectGraphJson)) taken.Add(s.EffectGraphJson!);

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

            List<BehaviorCount> fought;
            try { fought = JsonSerializer.Deserialize<List<BehaviorCount>>(skill.BehaviorProfileJson) ?? new(); }
            catch (JsonException) { fought = new(); }

            // ADR 0007 Phase 4c: the AI composes the whole skill — name, description, and effect
            // GRAPH — in ONE call. The graph is the sole composed artifact (no primitive pass);
            // Avoid carries the actor-wide taken structures so the new skill stays distinct.
            var graphProfile = fought.Select(b => new ProjectAscension.SkillForge.BehaviorWeight(b.Behavior, b.Count)).ToList();
            var lineage = await RetrieveGraphLineageAsync(skill.DiscoveryId, ct); // RAG: evolve prior discoveries
            var startedAt = Stopwatch.GetTimestamp();
            var comp = await _graphComposer.ComposeAsync(
                new EffectGraphRequest(skill.Theme, graphProfile, new PowerBudget(skill.PowerBudget), request.Seed, taken.ToList(), lineage), ct);
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            skill.Attempts++;

            if (comp is not null)
            {
                skill.Name = comp.Name;
                skill.Description = comp.Description;
                skill.EffectGraphJson = EffectGraphJson.Serialize(comp.Graph);
                skill.PowerCost = EffectGraph.Cost(comp.Graph);
                // Delivery SHAPE from the graph's first Emit (the client also derives this); a
                // graphless/movement skill has no emit → the behavior heuristic covers the DTO.
                var delivery = FirstDelivery(comp.Graph);
                skill.Delivery = delivery ?? DeliveryHeuristics.ForBehavior(fought);

                // The canonical graph serialization is the structural signature — dedup against it
                // so no two discovered skills share a shape (the "duplicate skill" guard, now on the
                // graph instead of the primitive-kind set).
                taken.Add(skill.EffectGraphJson);

                // Manifestation follows the graph's structure (ADR 0007) — always available now.
                bool magicContext = SkillManifest.IsMagicContext(DeserializeTags(skill.ContextTagsJson));
                var manifestation = ManifestationFromGraph.Classify(comp.Graph, magicContext) ?? ManifestationKind.Command;
                skill.Manifestation = manifestation.ToString();

                // A command is invoked by a button combo the rule engine assigns deterministically,
                // made PREFIX-FREE against the actor's other commands so the client fires the instant
                // the combo completes. Weapons fire on the attack input.
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

                skill.Status = DiscoveryContentStatus.Ready;
                skill.ComposedAt = DateTime.UtcNow;

                _metrics.Completed(1, elapsedMs);
                _logger.LogInformation(
                    "Composed \"{Name}\" for discovery {DiscoveryId} in {ElapsedMs:F0}ms (cost {Cost}/{Budget}).",
                    skill.Name, skill.DiscoveryId, elapsedMs, skill.PowerCost, skill.PowerBudget);
            }
            else
            {
                // Leave Pending — retried on a later pass (defer, no fallback: ADR 0002).
                _metrics.Deferred(1, elapsedMs);
                _logger.LogWarning(
                    "Deferred composition for discovery {DiscoveryId} — no valid skill graph.", skill.DiscoveryId);
            }

            await _skills.UpdateAsync(skill, ct);
        }
    }

    public async Task<DiscoverySkillResponse?> GetByDiscoveryAsync(Guid discoveryId, CancellationToken ct = default)
    {
        var skill = await _skills.GetByDiscoveryIdAsync(discoveryId, ct);
        if (skill is null) return null;

        var contextTags = DeserializeTags(skill.ContextTagsJson);
        var behaviors = DeserializeTags(skill.BehaviorsJson);
        var invocationCombo = DeserializeTags(skill.InvocationComboJson);

        return new DiscoverySkillResponse(
            skill.DiscoveryId, skill.Status, skill.Name, skill.Description, skill.PowerCost,
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
            BehaviorProfile: profile, Seed: seed);
        return true;
    }
}
