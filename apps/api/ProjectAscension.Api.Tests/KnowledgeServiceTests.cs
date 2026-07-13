using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Effects;

namespace ProjectAscension.Api.Tests;

/// <summary>Knowledge licensing (ADR 0014): a license may be sold ONCE per discovery — the
/// server, not a client-held "already sold" set, is what makes a re-sell impossible. Price/
/// reputation come from the skill's own composed effect graph + DB-driven tuning.</summary>
public class KnowledgeServiceTests
{
    private const string Graph = "{\"trigger\":\"OnCast\",\"effect\":{\"kind\":\"Emit\",\"delivery\":\"Burst\",\"tier\":2}}";

    private sealed class FakeKnowledgeRepo : IKnowledgeRepository
    {
        public List<Knowledge> Items { get; } = new();
        public int UpdateCount { get; private set; }

        public Task AddAsync(Knowledge knowledge, CancellationToken ct = default)
        {
            Items.Add(knowledge);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Knowledge>> GetByOwnerAsync(Guid ownerActorId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Knowledge>>(Items.Where(k => k.OwnerActorId == ownerActorId).ToList());

        public Task<Knowledge?> GetByDiscoveryIdAsync(Guid discoveryId, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(k => k.DiscoveryId == discoveryId));

        public Task UpdateAsync(Knowledge knowledge, CancellationToken ct = default)
        {
            UpdateCount++;
            return Task.CompletedTask; // mutated in place
        }
    }

    private sealed class FakeDiscoverySkillRepo : IDiscoverySkillRepository
    {
        public List<DiscoverySkill> Items { get; } = new();

        public Task AddAsync(DiscoverySkill skill, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<DiscoverySkill?> GetByDiscoveryIdAsync(Guid discoveryId, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(s => s.DiscoveryId == discoveryId));
        public Task<IReadOnlyList<DiscoverySkill>> GetByDiscoveryIdsAsync(IEnumerable<Guid> discoveryIds, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<DiscoverySkill?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<DiscoverySkill>> GetPendingAsync(int limit, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<DiscoverySkill>> GetReadyAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(DiscoverySkill skill, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakePlayerProfileRepo : IPlayerProfileRepository
    {
        public PlayerProfile? Profile { get; set; }
        public Task<PlayerProfile?> GetAsync(CancellationToken ct = default) => Task.FromResult(Profile);
        public Task UpdateAsync(PlayerProfile profile, CancellationToken ct = default) { Profile = profile; return Task.CompletedTask; }
    }

    private sealed class FakeEconomyTuningRepo : IEconomyTuningRepository
    {
        public EconomyTuning? Tuning { get; set; }
        public Task<EconomyTuning?> GetAsync(CancellationToken ct = default) => Task.FromResult(Tuning);
    }

    private static (KnowledgeService svc, FakeKnowledgeRepo knowledge, FakeDiscoverySkillRepo skills, FakePlayerProfileRepo players, FakeEconomyTuningRepo tuning)
        NewService()
    {
        var knowledge = new FakeKnowledgeRepo();
        var skills = new FakeDiscoverySkillRepo();
        var players = new FakePlayerProfileRepo { Profile = new PlayerProfile { Id = 1 } };
        var tuning = new FakeEconomyTuningRepo();
        return (new KnowledgeService(knowledge, skills, players, tuning), knowledge, skills, players, tuning);
    }

    private static Knowledge Owned(Guid actor, Guid discovery, bool licensed = false) => new()
    {
        Id = Guid.NewGuid(),
        DiscoveryId = discovery,
        OwnerActorId = actor,
        CreatedAt = DateTime.UtcNow,
        Licensed = licensed,
    };

    private static DiscoverySkill ReadySkill(Guid discovery, string? graph = Graph) => new()
    {
        Id = Guid.NewGuid(),
        DiscoveryId = discovery,
        Status = DiscoveryContentStatus.Ready,
        EffectGraphJson = graph,
    };

    [Fact]
    public async Task License_NotOwned_ReturnsNotFound()
    {
        var (svc, knowledge, skills, _, _) = NewService();
        var actor = Guid.NewGuid();
        var discovery = Guid.NewGuid();
        knowledge.Items.Add(Owned(Guid.NewGuid(), discovery)); // owned by someone else
        skills.Items.Add(ReadySkill(discovery));

        var result = await svc.LicenseAsync(new LicenseKnowledgeRequest(actor, discovery));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task License_AlreadyLicensed_ReturnsConflict_AndPaysNothing()
    {
        var (svc, knowledge, skills, players, _) = NewService();
        var actor = Guid.NewGuid();
        var discovery = Guid.NewGuid();
        knowledge.Items.Add(Owned(actor, discovery, licensed: true));
        skills.Items.Add(ReadySkill(discovery));
        int before = players.Profile!.Currency;

        var result = await svc.LicenseAsync(new LicenseKnowledgeRequest(actor, discovery));

        Assert.False(result.IsSuccess);
        Assert.Equal(before, players.Profile.Currency);
        Assert.Equal(0, knowledge.UpdateCount);
    }

    [Fact]
    public async Task License_NotComposedYet_ReturnsInvalid()
    {
        var (svc, knowledge, skills, _, _) = NewService();
        var actor = Guid.NewGuid();
        var discovery = Guid.NewGuid();
        knowledge.Items.Add(Owned(actor, discovery));
        skills.Items.Add(new DiscoverySkill { DiscoveryId = discovery, Status = DiscoveryContentStatus.Pending });

        var result = await svc.LicenseAsync(new LicenseKnowledgeRequest(actor, discovery));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task License_Owned_PaysServerComputedPriceAndReputation_AndLocksItToOnce()
    {
        var (svc, knowledge, skills, players, tuning) = NewService();
        tuning.Tuning = new EconomyTuning { Id = 1, KnowledgeGoldPerPoint = 6, KnowledgePointsPerRep = 5 };
        var actor = Guid.NewGuid();
        var discovery = Guid.NewGuid();
        var k = Owned(actor, discovery);
        knowledge.Items.Add(k);
        skills.Items.Add(ReadySkill(discovery));

        var graph = EffectGraphReader.Parse(Graph)!;
        int expectedPrice = KnowledgeValuation.LicensePrice(graph, 6);
        int expectedRep = KnowledgeValuation.LicenseReputation(graph, 5);

        var result = await svc.LicenseAsync(new LicenseKnowledgeRequest(actor, discovery));

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedPrice, result.Value!.Currency);
        Assert.Equal(expectedRep, result.Value.Reputation);
        Assert.True(k.Licensed);
        Assert.NotNull(k.LicensedAt);

        // Selling it again is now impossible — the flag persisted, not a client-held set. The error
        // is CONFLICT, which KnowledgeController maps to HTTP 409 (ResultExtensions.ToActionResult).
        var replay = await svc.LicenseAsync(new LicenseKnowledgeRequest(actor, discovery));
        Assert.False(replay.IsSuccess);
        Assert.Equal("CONFLICT", replay.Error.Code);
        Assert.Equal(expectedPrice, players.Profile!.Currency); // not paid twice
    }
}
