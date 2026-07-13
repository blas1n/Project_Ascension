using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Tests;

public class SkillCompositionServiceTests
{
    // The region is part of the discovery ladder (ADR 0011: 환경이 발견을 다르게 만든다). Real play stays
    // in one region across a session, so the fixtures must too — a fresh Guid per call would make every
    // evaluation a different place.
    private static readonly Guid Region = Guid.NewGuid();

    private sealed class FakeSkillRepo : IDiscoverySkillRepository
    {
        public List<DiscoverySkill> Skills { get; } = new();

        public Task AddAsync(DiscoverySkill skill, CancellationToken ct = default)
        {
            Skills.Add(skill);
            return Task.CompletedTask;
        }

        public Task<DiscoverySkill?> GetByDiscoveryIdAsync(Guid discoveryId, CancellationToken ct = default)
            => Task.FromResult(Skills.FirstOrDefault(s => s.DiscoveryId == discoveryId));

        public Task<IReadOnlyList<DiscoverySkill>> GetByDiscoveryIdsAsync(IEnumerable<Guid> discoveryIds, CancellationToken ct = default)
        {
            var ids = discoveryIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<DiscoverySkill>>(Skills.Where(s => ids.Contains(s.DiscoveryId)).ToList());
        }

        public Task<DiscoverySkill?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
            => Task.FromResult(Skills.FirstOrDefault(s => s.IdempotencyKey == key));

        public Task<IReadOnlyList<DiscoverySkill>> GetPendingAsync(int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DiscoverySkill>>(
                Skills.Where(s => s.Status == DiscoveryContentStatus.Pending).Take(limit).ToList());

        public Task<IReadOnlyList<DiscoverySkill>> GetReadyAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DiscoverySkill>>(
                Skills.Where(s => s.Status == DiscoveryContentStatus.Ready).ToList());

        public Task UpdateAsync(DiscoverySkill skill, CancellationToken ct = default) => Task.CompletedTask; // mutated in place
    }

    private sealed class FakeDiscoveryRepo : IDiscoveryRepository
    {
        public List<Discovery> Discoveries { get; } = new();

        public Task AddAsync(Discovery discovery, CancellationToken ct = default)
        {
            Discoveries.Add(discovery);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Discovery>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Discovery>>(Discoveries.Where(d => d.DiscovererActorId == actorId).ToList());

        public Task<DiscoveryProgress?> GetProgressAsync(Guid actorId, Guid candidateId, CancellationToken ct = default)
            => Task.FromResult<DiscoveryProgress?>(null);

        public Task UpsertProgressAsync(DiscoveryProgress progress, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeKnowledgeRepo : IKnowledgeRepository
    {
        public List<Knowledge> Items { get; } = new();

        public Task AddAsync(Knowledge knowledge, CancellationToken ct = default)
        {
            Items.Add(knowledge);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Knowledge>> GetByOwnerAsync(Guid ownerActorId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Knowledge>>(Items.Where(k => k.OwnerActorId == ownerActorId).ToList());
    }

    private sealed class FakeTuningProvider : IDiscoveryTuningProvider
    {
        public Task<DiscoveryTuning> GetAsync(CancellationToken ct = default) => Task.FromResult(DiscoveryTuning.Default);
    }

    private sealed class FakeLineageRepo : IDiscoveryLineageRepository
    {
        public List<DiscoveryLineage> Edges { get; } = new();

        public Task AddEdgesAsync(IEnumerable<DiscoveryLineage> edges, CancellationToken ct = default)
        {
            Edges.AddRange(edges);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DiscoveryLineage>> GetByChildAsync(Guid childDiscoveryId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DiscoveryLineage>>(Edges.Where(e => e.ChildDiscoveryId == childDiscoveryId).ToList());
    }

    // Captures the request the graph composer receives (to assert the Avoid dedup set), delegating
    // to the deterministic stub for the actual composition.
    private sealed class CapturingGraphComposer : IEffectGraphComposer
    {
        private readonly StubEffectGraphComposer _inner = new();
        public EffectGraphRequest? Last { get; private set; }

        public Task<SkillGraphComposition?> ComposeAsync(EffectGraphRequest request, CancellationToken ct = default)
        {
            Last = request;
            return _inner.ComposeAsync(request, ct);
        }
    }

    private static SkillCompositionService Service(
        FakeDiscoveryRepo discoveries, FakeSkillRepo skills, FakeKnowledgeRepo knowledge, CompositionMetrics metrics,
        FakeLineageRepo? lineage = null, IEffectGraphComposer? graphComposer = null)
        => new(discoveries, skills, knowledge, lineage ?? new FakeLineageRepo(), new FakeTuningProvider(),
            graphComposer ?? new StubEffectGraphComposer(), metrics, NullLogger<SkillCompositionService>.Instance);

    private static DiscoverySkill Pending() => new()
    {
        Id = Guid.NewGuid(),
        DiscoveryId = Guid.NewGuid(),
        Status = DiscoveryContentStatus.Pending,
        Theme = "arcane fire",
        ContextTagsJson = "[\"arcane\"]",
        PrimaryBehavior = "Projectile",
        PowerBudget = 30,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task ComposePending_ComposesAndRecordsCompletedMetric()
    {
        var skills = new FakeSkillRepo();
        skills.Skills.Add(Pending());

        using var metrics = new CompositionMetrics();
        long completed = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == CompositionMetrics.MeterName && instrument.Name == "discovery.composition.completed")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => Interlocked.Add(ref completed, value));
        listener.Start();

        await Service(new FakeDiscoveryRepo(), skills, new FakeKnowledgeRepo(), metrics).ComposePendingAsync(10);

        Assert.Equal(DiscoveryContentStatus.Ready, skills.Skills[0].Status);
        Assert.False(string.IsNullOrEmpty(skills.Skills[0].Name));
        Assert.Equal(1, completed);

        // ADR 0007: a composed skill also carries a valid effect graph (the runtime structure).
        var graphJson = skills.Skills[0].EffectGraphJson;
        Assert.False(string.IsNullOrEmpty(graphJson));
        var graph = EffectGraphJson.Parse(graphJson!);
        Assert.NotNull(graph);
        Assert.True(EffectGraphValidator.Validate(graph!, new PowerBudget(skills.Skills[0].PowerBudget)).IsValid);
    }

    [Fact]
    public async Task ComposePending_SeedsAvoidWithExistingReadySkillGraphs()
    {
        // Actor-wide dedup (ADR 0007 Phase 4c): a new composition must be told to avoid the GRAPH
        // signature of an already-composed skill — even one on a different line — so it can't
        // reproduce the same structure under a different name (the "two identical skills" bug).
        const string existingGraph = "{\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Emit\",\"delivery\":\"Beam\",\"tier\":1}}";
        var skills = new FakeSkillRepo();
        skills.Skills.Add(new DiscoverySkill
        {
            Id = Guid.NewGuid(),
            DiscoveryId = Guid.NewGuid(),
            Status = DiscoveryContentStatus.Ready,
            Name = "Existing",
            EffectGraphJson = existingGraph,
            CreatedAt = DateTime.UtcNow,
        });
        skills.Skills.Add(Pending());

        var composer = new CapturingGraphComposer();
        using var metrics = new CompositionMetrics();
        await Service(new FakeDiscoveryRepo(), skills, new FakeKnowledgeRepo(), metrics, graphComposer: composer)
            .ComposePendingAsync(10);

        Assert.NotNull(composer.Last);
        Assert.Contains(existingGraph, composer.Last!.Avoid ?? new List<string>());
    }

    [Fact]
    public async Task Trigger_IsIdempotentByKey()
    {
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        var knowledge = new FakeKnowledgeRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, knowledge, metrics);

        var req = new TriggerDiscoveryRequest(
            Guid.NewGuid(), Guid.NewGuid(), DiscoveryType.Skill, "t", new[] { "arcane" }, "Projectile", "Rare",
            IdempotencyKey: "k1");

        var first = await service.TriggerAsync(req);
        var again = await service.TriggerAsync(req);                          // same key
        var other = await service.TriggerAsync(req with { IdempotencyKey = "k2" });

        Assert.Equal(first, again);              // same discovery returned, not a duplicate
        Assert.NotEqual(first, other);           // a different key makes a new discovery
        Assert.Equal(2, skills.Skills.Count);    // only k1 and k2 created
        Assert.Equal(2, discoveries.Discoveries.Count);
        Assert.Equal(2, knowledge.Items.Count);  // ownership created per new discovery, not on the idempotent repeat
    }

    [Fact]
    public async Task Trigger_MakesDiscovererTheOwner()
    {
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        var knowledge = new FakeKnowledgeRepo();
        using var metrics = new CompositionMetrics();
        var actor = Guid.NewGuid();

        var id = await Service(discoveries, skills, knowledge, metrics).TriggerAsync(new TriggerDiscoveryRequest(
            actor, Guid.NewGuid(), DiscoveryType.Skill, "t", new[] { "arcane" }, "Projectile", "Rare"));

        var owned = Assert.Single(knowledge.Items);
        Assert.Equal(actor, owned.OwnerActorId);
        Assert.Equal(id, owned.DiscoveryId);
    }

    private static EvaluateTriggerRequest Eval(int jumpCount, Guid? actor = null)
        => new(actor ?? Guid.NewGuid(), Region, DiscoveryType.Skill, "t", new[] { "arcane" }, "Projectile",
            new[] { new BehaviorCount("Jump", jumpCount) }, Persistence: 0);

    [Fact]
    public async Task Evaluate_BelowThreshold_DoesNotFire()
    {
        var discoveries = new FakeDiscoveryRepo();
        using var metrics = new CompositionMetrics();

        var result = await Service(discoveries, new FakeSkillRepo(), new FakeKnowledgeRepo(), metrics)
            .EvaluateAndTriggerAsync(Eval(jumpCount: 10));

        Assert.False(result.Fired);
        Assert.Null(result.DiscoveryId);
        Assert.Empty(discoveries.Discoveries);
    }

    [Fact]
    public async Task Evaluate_AboveThreshold_FiresAndIsIdempotentPerRegion()
    {
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, new FakeKnowledgeRepo(), metrics);

        var req = Eval(jumpCount: 200); // score 200

        // However well you played, a style you have never touched starts at COMMON (ADR 0010) — that is
        // what makes breadth pay in quantity and depth pay in rarity.
        var first = await service.EvaluateAndTriggerAsync(req);
        Assert.True(first.Fired);
        Assert.NotNull(first.DiscoveryId);

        // The same play again climbs at most ONE rung, and only if it is worth it (Uncommon needs 150).
        var second = await service.EvaluateAndTriggerAsync(req);
        Assert.True(second.Fired);

        // And then it STALLS: Rare demands 225, and repeating yourself will never get there. The only
        // way on is to play better.
        Assert.False((await service.EvaluateAndTriggerAsync(req)).Fired);
        Assert.False((await service.EvaluateAndTriggerAsync(req)).Fired);

        // Two rungs, earned in order — not an endless stream of near-identical skills.
        Assert.Equal(2, discoveries.Discoveries.Count);
        Assert.Equal(2, skills.Skills.Count);
    }

    private static EvaluateTriggerRequest EvalCtx(Guid actor, string[] contextTags, int jumpCount)
        => new(actor, Region, DiscoveryType.Skill, "t", contextTags, "Projectile",
            new[] { new BehaviorCount("Jump", jumpCount) }, Persistence: 0);

    private static EvaluateTriggerRequest EvalBehavior(Guid actor, params BehaviorCount[] behaviors)
        => new(actor, Region, DiscoveryType.Skill, "t", new[] { "arcane" }, "Beam",
            behaviors, Persistence: 0);

    [Fact]
    public async Task Evaluate_SameAttackStyleWithStrayMovement_ClaimsOnce()
    {
        // The duplicate fix: real play flickers (a stray jump, a rising score), but the
        // same attack style must claim ONCE — not a fresh near-identical discovery per window.
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, new FakeKnowledgeRepo(), metrics);

        var actor = Guid.NewGuid();
        EvaluateTriggerRequest Ranged(int persistence, params BehaviorCount[] extra) =>
            new(actor, Region, DiscoveryType.Skill, "t", new[] { "arcane" }, "Beam",
                new[] { new BehaviorCount("RangedAttack", 300) }.Concat(extra).ToArray(), persistence);

        var plain = await service.EvaluateAndTriggerAsync(Ranged(2));
        var withJump = await service.EvaluateAndTriggerAsync(Ranged(6, new BehaviorCount("Jump", 40)));
        var withMoreJump = await service.EvaluateAndTriggerAsync(Ranged(12, new BehaviorCount("Jump", 90)));

        Assert.True(plain.Fired);

        // A stray jump must not fragment one play into a second ladder. The variants may climb
        // the ladder (that is the progression), but they climb THE SAME ONE.
        Assert.Single(skills.Skills.Select(StyleOf).Distinct());
    }

    [Fact]
    public async Task Evaluate_BreadthPaysInQuantity_DepthPaysInRarity()
    {
        // The bargain (ADR 0010). A player who spreads across many styles must end up with MANY
        // ORDINARY skills; a player who stays with one must end up with FEW RARE ones. Neither is
        // cheated — but they must not be able to have both.
        //
        // The mechanism: a fresh style starts at COMMON however brilliantly you played, and you climb
        // ONE rung at a time, each demanding exponentially more.
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, new FakeKnowledgeRepo(), metrics);

        var actor = Guid.NewGuid();
        EvaluateTriggerRequest Style(string tag, int ranged) =>
            new(actor, Region, DiscoveryType.Skill, "t", new[] { tag }, "Beam",
                new[] { new BehaviorCount("RangedAttack", ranged) }, Persistence: 0);

        // BREADTH: a brilliant session (score 600) in a style he has never touched. He still gets a
        // COMMON. Excellence cannot buy its way to rarity in a place you have not been.
        var breadth = await service.EvaluateAndTriggerAsync(Style("arcane", 300));
        Assert.True(breadth.Fired);
        Assert.Equal(Rarity.Common.ToString(), RarityOf(skills, breadth.DiscoveryId!.Value));

        // DEPTH: the same excellence, applied again and again to the SAME style, climbs — and each rung
        // costs exponentially more, so it climbs only as far as the play deserves.
        for (int i = 0; i < 6; i++) await service.EvaluateAndTriggerAsync(Style("arcane", 300));

        var deepest = skills.Skills
            .Select(sk => Enum.Parse<Rarity>(sk.IdempotencyKey!.Split(':').Last()))
            .Max();
        Assert.True(deepest >= Rarity.Epic, $"staying with one style must reach rarity; got {deepest}");

        // And a WEAK session in that style now discovers nothing at all — not a lesser skill, not a
        // consolation. The lower rungs are behind him and the next one is out of reach.
        Assert.False((await service.EvaluateAndTriggerAsync(Style("arcane", 60))).Fired);
    }

    /// <summary>The style ladder a skill was claimed on — the claim key minus its rarity rung.</summary>
    private static string StyleOf(DiscoverySkill s)
    {
        var k = s.IdempotencyKey!;
        return k[..k.LastIndexOf(':')];
    }

    private static string RarityOf(FakeSkillRepo skills, Guid discoveryId)
        => skills.Skills.First(s => s.DiscoveryId == discoveryId).IdempotencyKey!.Split(':').Last();

    [Fact]
    public async Task Evaluate_SameCombinationFoughtDifferently_ClaimsADistinctDiscovery()
    {
        // The whole point of behavior-driven composition: the same equipment fought a
        // different way must be a NEW discovery, not blocked by the first claim.
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, new FakeKnowledgeRepo(), metrics);

        var actor = Guid.NewGuid();
        var charging = await service.EvaluateAndTriggerAsync(
            EvalBehavior(actor, new BehaviorCount("ChargedAttack", 200)));
        var skirmishing = await service.EvaluateAndTriggerAsync(
            EvalBehavior(actor, new BehaviorCount("RangedAttack", 120), new BehaviorCount("Jump", 110)));
        // The same play again climbs its OWN ladder — it does not spill into the other one.
        var chargingAgain = await service.EvaluateAndTriggerAsync(
            EvalBehavior(actor, new BehaviorCount("ChargedAttack", 200)));

        Assert.True(charging.Fired);
        Assert.True(skirmishing.Fired);

        // The point, stated exactly: fighting the same combination a DIFFERENT way lands on a DIFFERENT
        // ladder. (Climbing within a ladder is the progression; forking between them is the variety.)
        var chargingStyle = StyleOf(skills.Skills.First(k => k.DiscoveryId == charging.DiscoveryId));
        var skirmishStyle = StyleOf(skills.Skills.First(k => k.DiscoveryId == skirmishing.DiscoveryId));
        Assert.NotEqual(chargingStyle, skirmishStyle);          // different play style → distinct discovery

        // Two ladders, and the charging one has climbed a rung on its repeat — progression within a
        // style, variety across styles. Both, and neither at the other's expense.
        Assert.Equal(2, skills.Skills.Select(StyleOf).Distinct().Count());
        Assert.Equal(3, discoveries.Discoveries.Count);
    }

    [Fact]
    public async Task Evaluate_VolatileCatalystTags_DoNotFragmentTheClaim()
    {
        // Regression: transient monster:* tags (a rolling kill window) and the player's own
        // spell:* tags (a discovery feedback loop) shifted every flush window, so the claim
        // key changed each time and a fresh "first discovery" was minted every ~5s — a
        // stream of near-identical skills. The same essential combination must claim once.
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, new FakeKnowledgeRepo(), metrics);

        var actor = Guid.NewGuid();

        // jumpCount clears the rarity rung comfortably: the volatile tags wobble the score by a few
        // points (monster:elite 14 vs monster:melee 6), and the claim must not fragment on that.
        var first = await service.EvaluateAndTriggerAsync(
            EvalCtx(actor, new[] { "arcane", "monster:elite" }, jumpCount: 400));
        var withDifferentCatalysts = await service.EvaluateAndTriggerAsync(
            EvalCtx(actor, new[] { "arcane", "monster:melee", "spell:flame-bullet" }, jumpCount: 400));

        Assert.True(first.Fired);

        // The volatile tags must not open a SECOND ladder. Whatever happens next (it may climb a rung),
        // it happens on the SAME style — the essential combination is unchanged.
        Assert.Single(skills.Skills.Select(StyleOf).Distinct());
    }

    [Fact]
    public async Task Evaluate_RecordsLineageFromPriorKnowledge()
    {
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        var knowledge = new FakeKnowledgeRepo();
        var lineage = new FakeLineageRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, knowledge, metrics, lineage);
        var actor = Guid.NewGuid();

        // First discovery in the "fire" space — no prior knowledge, so no parents.
        var first = await service.EvaluateAndTriggerAsync(EvalCtx(actor, new[] { "fire" }, 200));
        // Second in the same space (shares "fire") — builds on the first.
        var second = await service.EvaluateAndTriggerAsync(EvalCtx(actor, new[] { "fire", "compression" }, 200));

        Assert.True(first.Fired);
        Assert.True(second.Fired);
        Assert.NotEqual(first.DiscoveryId, second.DiscoveryId);

        var edge = Assert.Single(lineage.Edges);
        Assert.Equal(second.DiscoveryId, edge.ChildDiscoveryId);
        Assert.Equal(first.DiscoveryId, edge.ParentDiscoveryId);
    }

    [Fact]
    public async Task GetLineage_ReturnsAncestorsNearestFirst()
    {
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        var knowledge = new FakeKnowledgeRepo();
        var lineage = new FakeLineageRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, knowledge, metrics, lineage);
        var actor = Guid.NewGuid();

        var first = await service.EvaluateAndTriggerAsync(EvalCtx(actor, new[] { "fire" }, 200));
        var second = await service.EvaluateAndTriggerAsync(EvalCtx(actor, new[] { "fire", "compression" }, 200));

        var result = await service.GetLineageAsync(second.DiscoveryId!.Value);

        Assert.Equal(second.DiscoveryId, result.DiscoveryId);
        var ancestor = Assert.Single(result.Ancestors);
        Assert.Equal(first.DiscoveryId, ancestor.DiscoveryId);
    }

    // The graph is the sole artifact now (ADR 0007 Phase 4c): a composition with no valid graph
    // defers the discovery (Pending), with no primitive fallback (ADR 0002).
    private sealed class NullGraphComposer : IEffectGraphComposer
    {
        public Task<SkillGraphComposition?> ComposeAsync(EffectGraphRequest request, CancellationToken ct = default)
            => Task.FromResult<SkillGraphComposition?>(null);
    }

    [Fact]
    public async Task ComposePending_IsGraphOnly_AndDefersWhenNoGraph()
    {
        // Success path: graph set (the sole artifact), name from the composer.
        var ready = new FakeSkillRepo();
        ready.Skills.Add(Pending());
        using var m1 = new CompositionMetrics();
        await Service(new FakeDiscoveryRepo(), ready, new FakeKnowledgeRepo(), m1).ComposePendingAsync(10);
        Assert.Equal(DiscoveryContentStatus.Ready, ready.Skills[0].Status);
        Assert.False(string.IsNullOrEmpty(ready.Skills[0].EffectGraphJson));

        // No-graph path: stays Pending (deferred), attempt counted.
        var deferred = new FakeSkillRepo();
        deferred.Skills.Add(Pending());
        using var m2 = new CompositionMetrics();
        await Service(new FakeDiscoveryRepo(), deferred, new FakeKnowledgeRepo(), m2, graphComposer: new NullGraphComposer())
            .ComposePendingAsync(10);
        Assert.Equal(DiscoveryContentStatus.Pending, deferred.Skills[0].Status);
        Assert.True(deferred.Skills[0].Attempts > 0);
    }

    [Fact]
    public async Task ComposePending_InjectsGraphLineage()
    {
        // RAG (ADR 0007): a pending child is composed with its Ready ancestors' name/description/
        // graph so the AI evolves prior discoveries rather than starting cold.
        var skills = new FakeSkillRepo();
        var lineage = new FakeLineageRepo();
        var composer = new CapturingGraphComposer();
        using var metrics = new CompositionMetrics();

        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        skills.Skills.Add(new DiscoverySkill
        {
            Id = Guid.NewGuid(),
            DiscoveryId = parentId,
            Status = DiscoveryContentStatus.Ready,
            Name = "Emberbrand",
            Description = "A dart of living flame.",
            EffectGraphJson = "{\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Emit\",\"delivery\":\"Projectile\",\"tier\":1}}",
            CreatedAt = DateTime.UtcNow,
        });
        var pending = Pending();
        pending.DiscoveryId = childId;
        skills.Skills.Add(pending);
        lineage.Edges.Add(new DiscoveryLineage { ChildDiscoveryId = childId, ParentDiscoveryId = parentId });

        await Service(new FakeDiscoveryRepo(), skills, new FakeKnowledgeRepo(), metrics, lineage, composer)
            .ComposePendingAsync(10);

        Assert.NotNull(composer.Last);
        var prior = Assert.Single(composer.Last!.Lineage!);
        Assert.Equal("Emberbrand", prior.Name);
    }
}
