using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectAscension.Api.Services;
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

        public Task<IReadOnlyList<DiscoverySkill>> GetPendingAsync(int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DiscoverySkill>>(
                Skills.Where(s => s.Status == DiscoveryContentStatus.Pending).Take(limit).ToList());

        public Task UpdateAsync(DiscoverySkill skill, CancellationToken ct = default) => Task.CompletedTask; // mutated in place
    }

    private sealed class UnusedDiscoveryRepo : IDiscoveryRepository
    {
        public Task AddAsync(Discovery discovery, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Discovery>> GetByActorAsync(Guid actorId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<DiscoveryProgress?> GetProgressAsync(Guid actorId, Guid candidateId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpsertProgressAsync(DiscoveryProgress progress, CancellationToken ct = default) => throw new NotSupportedException();
    }

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

        var service = new SkillCompositionService(
            new UnusedDiscoveryRepo(), skills, new StubSkillComposer(), metrics,
            NullLogger<SkillCompositionService>.Instance);

        await service.ComposePendingAsync(10);

        Assert.Equal(DiscoveryContentStatus.Ready, skills.Skills[0].Status);
        Assert.False(string.IsNullOrEmpty(skills.Skills[0].Name));
        Assert.Equal(1, completed);
    }
}
