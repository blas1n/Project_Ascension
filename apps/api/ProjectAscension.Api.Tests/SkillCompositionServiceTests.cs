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

        public Task<Knowledge?> GetByDiscoveryIdAsync(Guid discoveryId, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(k => k.DiscoveryId == discoveryId));

        public Task UpdateAsync(Knowledge knowledge, CancellationToken ct = default) => Task.CompletedTask;
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

    // Hands back a scripted sequence of results, one per call — models a composer whose first
    // (few) attempt(s) collide with something taken and a later one doesn't, the way a real LLM's
    // retry (a different seed each time) is meant to behave.
    private sealed class SequenceGraphComposer : IEffectGraphComposer
    {
        private readonly Queue<SkillGraphComposition?> _results;
        public List<EffectGraphRequest> Requests { get; } = new();

        public SequenceGraphComposer(params SkillGraphComposition?[] results) => _results = new(results);

        public Task<SkillGraphComposition?> ComposeAsync(EffectGraphRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.FromResult(_results.Count > 0 ? _results.Dequeue() : null);
        }
    }

    // A composer that NEVER produces anything new — every call returns the same graph shape (under
    // a fresh name each time, which is exactly what let "Aerial Convergence" freeze into the dev DB
    // four times: the old code checked nothing, so a renamed duplicate sailed through as Ready).
    private sealed class AlwaysSameGraphComposer : IEffectGraphComposer
    {
        private readonly EffectNode _graph;
        private readonly string _namePrefix;
        public int CallCount { get; private set; }

        public AlwaysSameGraphComposer(EffectNode graph, string namePrefix)
        {
            _graph = graph;
            _namePrefix = namePrefix;
        }

        public Task<SkillGraphComposition?> ComposeAsync(EffectGraphRequest request, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult<SkillGraphComposition?>(new SkillGraphComposition($"{_namePrefix} {CallCount}", "desc", _graph));
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

    // --- The duplicate-skill bug (project owner playtest report, 2026-07): the dev DB showed the
    // SAME graph shape frozen as Ready under FOUR different names ("Aerial Convergence" x4), plus
    // more sharing a shape under other names ("Aerial Cascade", "Fusion Firepath", "Converging
    // Leap" x2). The Avoid/AvoidNames lists were only ever a PROMPT HINT — nothing checked the
    // composer's actual output against them, so a model (or, offline, the deterministic stub) that
    // ignored the hint sailed straight through to Ready. These tests pin the fix: the service must
    // verify the result and retry, and must defer (never persist a duplicate) if it can't get
    // something new. ---

    [Fact]
    public async Task ComposePending_RetriesOnDuplicateGraph_AndLandsOnAUniqueOne()
    {
        var duplicateGraph = new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Beam, 1));
        var duplicateGraphJson = EffectGraphJson.Serialize(duplicateGraph);
        var uniqueGraph = new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Nova, 1));

        var skills = new FakeSkillRepo();
        skills.Skills.Add(new DiscoverySkill
        {
            Id = Guid.NewGuid(),
            DiscoveryId = Guid.NewGuid(),
            Status = DiscoveryContentStatus.Ready,
            Name = "Existing",
            EffectGraphJson = duplicateGraphJson,
            CreatedAt = DateTime.UtcNow,
        });
        var pending = Pending();
        skills.Skills.Add(pending);

        // Attempt 0 collides on GRAPH with "Existing" (different name, same shape — precisely the
        // reproduced bug); attempt 1 is genuinely new and must be the one that lands.
        var composer = new SequenceGraphComposer(
            new SkillGraphComposition("Fresh Name", "desc", duplicateGraph),
            new SkillGraphComposition("Fresh Name 2", "desc", uniqueGraph));

        using var metrics = new CompositionMetrics();
        await Service(new FakeDiscoveryRepo(), skills, new FakeKnowledgeRepo(), metrics, graphComposer: composer)
            .ComposePendingAsync(10);

        Assert.Equal(DiscoveryContentStatus.Ready, pending.Status);
        Assert.Equal("Fresh Name 2", pending.Name);
        Assert.Equal(EffectGraphJson.Serialize(uniqueGraph), pending.EffectGraphJson);
        Assert.Equal(2, composer.Requests.Count); // it actually retried, not just accepted the first try
    }

    [Fact]
    public async Task ComposePending_RetriesOnDuplicateName_EvenWhenTheGraphDiffers()
    {
        // A rename of a duplicate is still a duplicate, but the converse matters too: a REUSED name
        // over a genuinely different mechanic is still not "your own skill" — identity, not just
        // mechanics, must be distinct.
        var existingGraph = new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Beam, 1));
        var skills = new FakeSkillRepo();
        skills.Skills.Add(new DiscoverySkill
        {
            Id = Guid.NewGuid(),
            DiscoveryId = Guid.NewGuid(),
            Status = DiscoveryContentStatus.Ready,
            Name = "Aerial Convergence",
            EffectGraphJson = EffectGraphJson.Serialize(existingGraph),
            CreatedAt = DateTime.UtcNow,
        });
        var pending = Pending();
        skills.Skills.Add(pending);

        var composer = new SequenceGraphComposer(
            new SkillGraphComposition("Aerial Convergence", "desc", new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Nova, 1))),
            new SkillGraphComposition("Skybound Reprise", "desc", new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Nova, 1))));

        using var metrics = new CompositionMetrics();
        await Service(new FakeDiscoveryRepo(), skills, new FakeKnowledgeRepo(), metrics, graphComposer: composer)
            .ComposePendingAsync(10);

        Assert.Equal(DiscoveryContentStatus.Ready, pending.Status);
        Assert.Equal("Skybound Reprise", pending.Name);
        Assert.Equal(2, composer.Requests.Count);
    }

    [Fact]
    public async Task ComposePending_DefersRatherThanPersistingADuplicate_WhenTheComposerNeverProducesSomethingNew()
    {
        var duplicateGraph = new Trigger(TriggerKind.OnCast, new Emit(EmitDelivery.Beam, 1));
        var duplicateGraphJson = EffectGraphJson.Serialize(duplicateGraph);

        var skills = new FakeSkillRepo();
        skills.Skills.Add(new DiscoverySkill
        {
            Id = Guid.NewGuid(),
            DiscoveryId = Guid.NewGuid(),
            Status = DiscoveryContentStatus.Ready,
            Name = "Existing",
            EffectGraphJson = duplicateGraphJson,
            CreatedAt = DateTime.UtcNow,
        });
        var pending = Pending();
        skills.Skills.Add(pending);

        var composer = new AlwaysSameGraphComposer(duplicateGraph, "Reused Shape");

        using var metrics = new CompositionMetrics();
        await Service(new FakeDiscoveryRepo(), skills, new FakeKnowledgeRepo(), metrics, graphComposer: composer)
            .ComposePendingAsync(10);

        // Deferred (ADR 0002: no deterministic fallback skill) — NOT frozen as a duplicate Ready row.
        Assert.Equal(DiscoveryContentStatus.Pending, pending.Status);
        Assert.Null(pending.Name);
        Assert.Null(pending.EffectGraphJson);
        Assert.True(pending.Attempts > 0);
        Assert.Single(skills.Skills, s => s.EffectGraphJson == duplicateGraphJson); // still just "Existing"
        Assert.Equal(3, composer.CallCount); // exhausted every retry rather than giving up on the first collision
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

    [Fact]
    public async Task GetByDiscoveryAsync_ReflectsLicensedFlag()
    {
        // The client restores/polls through this endpoint (GameSession, SkillCaster) — it must
        // carry the server-authoritative Licensed truth so the knowledge market never offers to
        // re-sell what would only 409 (the playtest bug this fixes).
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        var knowledge = new FakeKnowledgeRepo();
        using var metrics = new CompositionMetrics();
        var discoveryId = Guid.NewGuid();
        skills.Skills.Add(new DiscoverySkill
        {
            Id = Guid.NewGuid(),
            DiscoveryId = discoveryId,
            Status = DiscoveryContentStatus.Ready,
            Name = "Bolt",
            EffectGraphJson = "{\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Emit\",\"delivery\":\"Burst\",\"tier\":1}}",
            CreatedAt = DateTime.UtcNow,
        });
        var service = Service(discoveries, skills, knowledge, metrics);

        var beforeLicense = await service.GetByDiscoveryAsync(discoveryId);
        Assert.False(beforeLicense!.Licensed);

        knowledge.Items.Add(new Knowledge
        {
            Id = Guid.NewGuid(),
            DiscoveryId = discoveryId,
            OwnerActorId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Licensed = true,
            LicensedAt = DateTime.UtcNow,
        });

        var afterLicense = await service.GetByDiscoveryAsync(discoveryId);
        Assert.True(afterLicense!.Licensed);
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

        var req = Eval(jumpCount: 350); // score 350

        // However well you played, a style you have never touched starts at COMMON (ADR 0010) — that is
        // what makes breadth pay in quantity and depth pay in rarity.
        var first = await service.EvaluateAndTriggerAsync(req);
        Assert.True(first.Fired);
        Assert.NotNull(first.DiscoveryId);

        // The same play again climbs at most ONE rung, and only if it is worth it (Uncommon needs 300).
        var second = await service.EvaluateAndTriggerAsync(req);
        Assert.True(second.Fired);

        // And then it STALLS: Rare demands 450, and repeating yourself will never get there. The only
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

        // BREADTH: a brilliant session (score 800) in a style he has never touched. He still gets a
        // COMMON. Excellence cannot buy its way to rarity in a place you have not been.
        var breadth = await service.EvaluateAndTriggerAsync(Style("arcane", 400));
        Assert.True(breadth.Fired);
        Assert.Equal(Rarity.Common.ToString(), RarityOf(skills, breadth.DiscoveryId!.Value));

        // DEPTH: the same excellence, applied again and again to the SAME style, climbs — and each rung
        // costs exponentially more, so it climbs only as far as the play deserves.
        for (int i = 0; i < 6; i++) await service.EvaluateAndTriggerAsync(Style("arcane", 400));

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

        // "MeleeAttack", not "ChargedAttack": a charged shot is now a While:...@charged quality on the
        // act stream (ADR 0009), not its own raw BehaviorKind — "ChargedAttack" is a dead key nothing
        // emits any more (see FactorAndBehaviorVocabularyTests).
        var actor = Guid.NewGuid();
        var charging = await service.EvaluateAndTriggerAsync(
            EvalBehavior(actor, new BehaviorCount("MeleeAttack", 200)));
        var skirmishing = await service.EvaluateAndTriggerAsync(
            EvalBehavior(actor, new BehaviorCount("RangedAttack", 120), new BehaviorCount("Jump", 110)));
        // The same play again climbs its OWN ladder — it does not spill into the other one.
        var chargingAgain = await service.EvaluateAndTriggerAsync(
            EvalBehavior(actor, new BehaviorCount("MeleeAttack", 200)));

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
        // spell:* tags (a discovery feedback loop) used to shift every flush window, so the claim
        // key changed each time and a fresh "first discovery" was minted every ~5s — a stream of
        // near-identical skills. The fix (DiscoveryScarcity) goes further than filtering those two
        // prefixes: ContextTags is not part of the style key AT ALL any more (see RegionKey), so no
        // context tag — volatile or not — can ever fragment a claim.
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
    public async Task Evaluate_EquippingADiscoveredWeapon_DoesNotOpenANewLadder()
    {
        // The bug this guards (Bug 2, discovery-scarcity): equipping a freshly discovered weapon
        // used to add its own "spell:xxx" context tag to the loadout snapshot the claim key was
        // built from — a brand-new key, a brand-new ladder, and the first rung of any ladder is a
        // free Common. The fix keys on the BEHAVIOURS instead: using the pistol alone, before or
        // after equipping (but not USING) a self-forged weapon, must land on the same ladder.
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, new FakeKnowledgeRepo(), metrics);
        var actor = Guid.NewGuid();

        EvaluateTriggerRequest Fire(IReadOnlyList<string> tags) => new(
            actor, Region, DiscoveryType.Skill, "t", tags, "Projectile",
            new[] { new BehaviorCount("Use:firearm", 1), new BehaviorCount("RangedAttack", 400) },
            Persistence: 0);

        // Before the discovery: loadout is just the pistol.
        var before = await service.EvaluateAndTriggerAsync(Fire(new[] { "firearm" }));
        // After equipping the freshly discovered weapon in the OTHER hand — its tag joins the
        // context snapshot — but the player still only USED the pistol this window.
        var after = await service.EvaluateAndTriggerAsync(Fire(new[] { "firearm", "spell:flame-lance" }));

        Assert.True(before.Fired);
        Assert.Single(skills.Skills.Select(StyleOf).Distinct()); // one ladder, not two
    }

    [Fact]
    public async Task Evaluate_CarryingAnUnusedWeapon_DoesNotForkTheStyle()
    {
        // A catalyst sitting in the other hand, never fired, must not count. The evidence is what
        // the BEHAVIOURS say happened — "Use:firearm" — not what ContextTags says was equipped.
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, new FakeKnowledgeRepo(), metrics);
        var actor = Guid.NewGuid();

        EvaluateTriggerRequest Fire(IReadOnlyList<string> tags) => new(
            actor, Region, DiscoveryType.Skill, "t", tags, "Projectile",
            new[] { new BehaviorCount("Use:firearm", 1), new BehaviorCount("RangedAttack", 400) },
            Persistence: 0);

        // Bare pistol, then pistol + an UNUSED catalyst in the other hand (never wove it in).
        var pistolOnly = await service.EvaluateAndTriggerAsync(Fire(new[] { "firearm" }));
        var withUnusedCatalyst = await service.EvaluateAndTriggerAsync(Fire(new[] { "firearm", "arcane" }));

        Assert.True(pistolOnly.Fired);
        Assert.Single(skills.Skills.Select(StyleOf).Distinct()); // carrying it changed nothing
    }

    [Fact]
    public async Task Evaluate_SamePlayInADifferentRegion_IsADifferentLadder()
    {
        // ADR 0011 §3: the region is part of the style. Unlike the loadout snapshot, this dimension
        // is meant to fork — the same play at the waterfall and in the crystal desert must not share
        // a ladder (discovery.md: "환경이 발견을 다르게 만든다").
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, new FakeKnowledgeRepo(), metrics);
        var actor = Guid.NewGuid();
        var otherRegion = Guid.NewGuid();

        EvaluateTriggerRequest Fire(Guid region) => new(
            actor, region, DiscoveryType.Skill, "t", Array.Empty<string>(), "Projectile",
            new[] { new BehaviorCount("Use:firearm", 1), new BehaviorCount("RangedAttack", 400) },
            Persistence: 0);

        var here = await service.EvaluateAndTriggerAsync(Fire(Region));
        var there = await service.EvaluateAndTriggerAsync(Fire(otherRegion));

        Assert.True(here.Fired);
        Assert.True(there.Fired);
        Assert.Equal(2, skills.Skills.Select(StyleOf).Distinct().Count()); // two ladders, one per region
    }

    [Fact]
    public async Task Evaluate_SustainedPlayInOneStyle_YieldsABoundedExponentiallySlowingStream()
    {
        // The property ADR 0010 exists to guarantee: repeating the SAME play in ONE style does not
        // yield a discovery every window — it yields a handful (at most one per rarity rung, five
        // total), each harder to reach than the last, and then NOTHING, however long you keep at it.
        var discoveries = new FakeDiscoveryRepo();
        var skills = new FakeSkillRepo();
        using var metrics = new CompositionMetrics();
        var service = Service(discoveries, skills, new FakeKnowledgeRepo(), metrics);
        var actor = Guid.NewGuid();

        // A single, FIXED "session" repeated many times — no escalation, exactly what grinding is. A
        // strong, sustained window (well above the first rung), never varied or improved upon.
        EvaluateTriggerRequest OneWindow() => new(
            actor, Region, DiscoveryType.Skill, "t", Array.Empty<string>(), "Projectile",
            new[]
            {
                new BehaviorCount("Use:firearm", 1),
                new BehaviorCount("RangedAttack", 300),
                new BehaviorCount("Chain:firearm", 3),
            },
            Persistence: 1);

        int fires = 0;
        for (int window = 0; window < 40; window++)
            if ((await service.EvaluateAndTriggerAsync(OneWindow())).Fired) fires++;

        // Bounded: never more than one discovery per rarity rung, however many windows of identical
        // play are thrown at it — and strictly fewer than the 40 windows played, so it is not one
        // discovery per window either.
        Assert.True(fires >= 1, "a strong sustained window should earn at least the first rung");
        Assert.True(fires <= Enum.GetValues<Rarity>().Length, $"grinding one style fired {fires} times");
        Assert.True(fires < 40, "grinding must exhaust itself, not fire every window");
        Assert.Single(skills.Skills.Select(StyleOf).Distinct()); // all on the SAME ladder
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
        var first = await service.EvaluateAndTriggerAsync(EvalCtx(actor, new[] { "fire" }, 400));
        // Second in the same space (shares "fire") — builds on the first.
        var second = await service.EvaluateAndTriggerAsync(EvalCtx(actor, new[] { "fire", "compression" }, 400));

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

        var first = await service.EvaluateAndTriggerAsync(EvalCtx(actor, new[] { "fire" }, 400));
        var second = await service.EvaluateAndTriggerAsync(EvalCtx(actor, new[] { "fire", "compression" }, 400));

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
