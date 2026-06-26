using System.Diagnostics.Metrics;
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

        public Task<DiscoverySkill?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
            => Task.FromResult(Skills.FirstOrDefault(s => s.IdempotencyKey == key));

        public Task<IReadOnlyList<DiscoverySkill>> GetPendingAsync(int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DiscoverySkill>>(
                Skills.Where(s => s.Status == DiscoveryContentStatus.Pending).Take(limit).ToList());

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

    private static SkillCompositionService Service(
        FakeDiscoveryRepo discoveries, FakeSkillRepo skills, FakeKnowledgeRepo knowledge, CompositionMetrics metrics)
        => new(discoveries, skills, knowledge, new FakeTuningProvider(), new StubSkillComposer(), metrics, NullLogger<SkillCompositionService>.Instance);

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
        => new(actor ?? Guid.NewGuid(), Guid.NewGuid(), DiscoveryType.Skill, "t", new[] { "arcane" }, "Projectile",
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

        var req = Eval(jumpCount: 200); // well past the fire threshold

        var first = await service.EvaluateAndTriggerAsync(req);
        var again = await service.EvaluateAndTriggerAsync(req); // same region → claimed once

        Assert.True(first.Fired);
        Assert.NotNull(first.DiscoveryId);
        Assert.Equal(first.DiscoveryId, again.DiscoveryId);
        Assert.Single(discoveries.Discoveries);
        Assert.Single(skills.Skills);
    }
}
