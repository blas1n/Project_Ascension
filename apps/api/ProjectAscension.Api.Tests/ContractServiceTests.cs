using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Tests;

/// <summary>Contracts are a priority pillar (they replace quests). This pins the
/// server-authoritative rules: the reward is calibrated + band-clamped (the server owns the
/// economy — ADR 0002), the objective count is bounded, flavor is player-authored-or-assisted,
/// the Open→Assigned→Completed state machine rejects out-of-order transitions, and the JSON
/// objective fields survive malformed data.</summary>
public class ContractServiceTests
{
    private sealed class FakeContractRepo : IContractRepository
    {
        public List<Contract> Contracts { get; } = new();
        public ContractRewardTuning? Tuning { get; set; }
        public int UpdateCount { get; private set; }

        public Task<IReadOnlyList<Contract>> GetByRegionAsync(Guid regionId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Contract>>(Contracts.ToList());

        public Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Contracts.FirstOrDefault(c => c.Id == id));

        public Task AddAsync(Contract contract, CancellationToken ct = default)
        {
            Contracts.Add(contract);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Contract contract, CancellationToken ct = default)
        {
            UpdateCount++;
            return Task.CompletedTask; // mutated in place
        }

        public Task<ContractRewardTuning?> GetRewardTuningAsync(CancellationToken ct = default)
            => Task.FromResult(Tuning);
    }

    private sealed class FakeMonsterRepo : IMonsterDefinitionRepository
    {
        public List<MonsterDefinition> Monsters { get; } = new();

        public Task<IReadOnlyList<MonsterDefinition>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MonsterDefinition>>(Monsters.ToList());
    }

    // Records whether the AI flavor path was taken and returns a distinctive marker.
    private sealed class RecordingFlavorComposer : IContractFlavorComposer
    {
        public int Calls { get; private set; }

        public Task<ContractFlavor> ComposeAsync(
            ContractPurpose purpose, string? target, int count,
            string fallbackTitle, string fallbackDescription, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new ContractFlavor("AI:" + fallbackTitle, "AI:" + fallbackDescription));
        }
    }

    private static (ContractService svc, FakeContractRepo repo, FakeMonsterRepo monsters, RecordingFlavorComposer flavor) NewService()
    {
        var repo = new FakeContractRepo();
        var monsters = new FakeMonsterRepo();
        var flavor = new RecordingFlavorComposer();
        return (new ContractService(repo, monsters, flavor), repo, monsters, flavor);
    }

    private static IssueContractRequest Issue(
        ContractPurpose purpose = ContractPurpose.Survey, string? target = null, int count = 2,
        int desiredReward = 50, int durationHours = 0, string? title = null, string? description = null)
        => new(Guid.NewGuid(), purpose, target, count, desiredReward, durationHours, title, description);

    // --- IssueAsync -------------------------------------------------------

    [Fact]
    public async Task Issue_EmptyIssuer_ReturnsInvalid()
    {
        var (svc, _, _, _) = NewService();

        var result = await svc.IssueAsync(new IssueContractRequest(
            Guid.Empty, ContractPurpose.Survey, null, 1, 50, 0));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Issue_NoAuthoredText_UsesAiComposer()
    {
        var (svc, _, _, flavor) = NewService();

        var result = await svc.IssueAsync(Issue());

        Assert.Equal(1, flavor.Calls);
        Assert.StartsWith("AI:", result.Value!.Title);
    }

    [Fact]
    public async Task Issue_AuthoredTitleAndDescription_SkipsAiAndTrims()
    {
        var (svc, _, _, flavor) = NewService();

        var result = await svc.IssueAsync(Issue(title: "  My Bounty  ", description: "  Clear them out.  "));

        Assert.Equal(0, flavor.Calls);
        Assert.Equal("My Bounty", result.Value!.Title);
        Assert.Equal("Clear them out.", result.Value.Description);
    }

    [Fact]
    public async Task Issue_PartialAuthored_FillsMissingFromTemplateNotAi()
    {
        var (svc, _, _, flavor) = NewService();

        // Only a title given → still "authored" (no AI); description auto-filled by template.
        var result = await svc.IssueAsync(Issue(purpose: ContractPurpose.Collection, count: 3, title: "Gather Run"));

        Assert.Equal(0, flavor.Calls);
        Assert.Equal("Gather Run", result.Value!.Title);
        Assert.Equal("Collect 3 samples in the frontier.", result.Value.Description);
    }

    [Theory]
    [InlineData(10, 35)]    // below band min (70% of 50) → clamp up to 35
    [InlineData(1000, 75)]  // above band max (150% of 50) → clamp down to 75
    [InlineData(60, 60)]    // inside band → kept
    public async Task Issue_ClampsRewardToBand(int desired, int expected)
    {
        var (svc, _, _, _) = NewService(); // no tuning → base 25, band 70..150; count 2 → suggested 50

        var result = await svc.IssueAsync(Issue(count: 2, desiredReward: desired));

        Assert.Equal(expected, result.Value!.RewardCurrency);
    }

    [Theory]
    [InlineData(0, 1)]    // non-positive → clamped up to 1
    [InlineData(50, 20)]  // over the cap → clamped to MaxObjectiveCount
    [InlineData(5, 5)]    // within range → kept
    public async Task Issue_ClampsObjectiveCount(int requested, int expected)
    {
        var (svc, _, _, _) = NewService();

        var result = await svc.IssueAsync(Issue(count: requested));

        Assert.Equal(expected, result.Value!.TargetCount);
    }

    [Fact]
    public async Task Issue_TargetedHunt_RecordsTarget()
    {
        var (svc, _, _, _) = NewService();

        var result = await svc.IssueAsync(Issue(purpose: ContractPurpose.Hunt, target: "elite", count: 4));

        Assert.Equal("elite", result.Value!.Target);
    }

    [Fact]
    public async Task Issue_NonHunt_HasNoTarget()
    {
        var (svc, _, _, _) = NewService();

        var result = await svc.IssueAsync(Issue(purpose: ContractPurpose.Survey, target: "elite"));

        Assert.True(string.IsNullOrEmpty(result.Value!.Target));
    }

    [Fact]
    public async Task Issue_CreatesOpenContractAndPersists()
    {
        var (svc, repo, _, _) = NewService();

        var result = await svc.IssueAsync(Issue(durationHours: 6));

        Assert.Equal(ContractStatus.Open, result.Value!.Status);
        var stored = Assert.Single(repo.Contracts);
        Assert.Equal(ContractStatus.Open, stored.Status);
        Assert.NotNull(stored.ExpiresAt); // duration > 0 sets a deadline
    }

    [Fact]
    public async Task Issue_NoDuration_HasNoExpiry()
    {
        var (svc, repo, _, _) = NewService();

        await svc.IssueAsync(Issue(durationHours: 0));

        Assert.Null(repo.Contracts.Single().ExpiresAt);
    }

    // --- Quote ------------------------------------------------------------

    [Fact]
    public async Task Quote_DefaultTuning_FlatBasePerCount()
    {
        var (svc, _, _, _) = NewService();

        var result = await svc.GetQuoteAsync(ContractPurpose.Survey, null, 3);

        // base 25 × 3 = 75; band 70..150%. MathF.Round is round-half-to-even.
        Assert.Equal(75, result.Value!.SuggestedReward);
        Assert.Equal(52, result.Value.MinReward);   // round(52.5) → 52 (even)
        Assert.Equal(112, result.Value.MaxReward);  // round(112.5) → 112 (even)
    }

    [Fact]
    public async Task Quote_UsesTuningRowWhenPresent()
    {
        var (svc, repo, _, _) = NewService();
        repo.Tuning = new ContractRewardTuning
        {
            Id = 1,
            BaseRewardPerCount = 10f,
            DifficultyScale = 0f,
            BandMinPercent = 50,
            BandMaxPercent = 200,
        };

        var result = await svc.GetQuoteAsync(ContractPurpose.Survey, null, 4);

        Assert.Equal(40, result.Value!.SuggestedReward); // 10 × 4
        Assert.Equal(20, result.Value.MinReward);        // 50%
        Assert.Equal(80, result.Value.MaxReward);        // 200%
    }

    [Fact]
    public async Task Quote_TargetedHunt_AddsMonsterDifficulty()
    {
        var (svc, _, monsters, _) = NewService();
        monsters.Monsters.Add(new MonsterDefinition { Key = "elite", MaxHealth = 100f, Damage = 10f });

        var result = await svc.GetQuoteAsync(ContractPurpose.Hunt, "elite", 1);

        // difficulty = (100 + 10×5) × 0.4 = 60; (25 + 60) × 1 = 85.
        Assert.Equal(85, result.Value!.SuggestedReward);
    }

    [Fact]
    public async Task Quote_UnknownTarget_NoDifficultyBonus()
    {
        var (svc, _, _, _) = NewService();

        var result = await svc.GetQuoteAsync(ContractPurpose.Hunt, "ghost", 2);

        Assert.Equal(50, result.Value!.SuggestedReward); // base only: 25 × 2
    }

    // --- State machine ----------------------------------------------------

    private static Contract OpenContract(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Status = ContractStatus.Open,
        ConditionsJson = "{\"targetCount\":1}",
        RewardJson = "{\"currency\":50}",
    };

    [Fact]
    public async Task Accept_Missing_ReturnsNotFound()
    {
        var (svc, _, _, _) = NewService();

        var result = await svc.AcceptAsync(Guid.NewGuid(), new AcceptContractRequest(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Accept_NotOpen_ReturnsConflict()
    {
        var (svc, repo, _, _) = NewService();
        var c = OpenContract();
        c.Status = ContractStatus.Assigned;
        repo.Contracts.Add(c);

        var result = await svc.AcceptAsync(c.Id, new AcceptContractRequest(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Accept_Open_AssignsToActor()
    {
        var (svc, repo, _, _) = NewService();
        var c = OpenContract();
        repo.Contracts.Add(c);
        var actor = Guid.NewGuid();

        var result = await svc.AcceptAsync(c.Id, new AcceptContractRequest(actor));

        Assert.True(result.IsSuccess);
        Assert.Equal(ContractStatus.Assigned, result.Value!.Status);
        Assert.Equal(actor, c.AssigneeActorId);
    }

    [Fact]
    public async Task Complete_NotAssigned_ReturnsConflict()
    {
        var (svc, repo, _, _) = NewService();
        var c = OpenContract(); // still Open, not Assigned
        repo.Contracts.Add(c);

        var result = await svc.CompleteAsync(c.Id);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Complete_Assigned_MarksCompleted()
    {
        var (svc, repo, _, _) = NewService();
        var c = OpenContract();
        c.Status = ContractStatus.Assigned;
        repo.Contracts.Add(c);

        var result = await svc.CompleteAsync(c.Id);

        Assert.Equal(ContractStatus.Completed, result.Value!.Status);
        Assert.NotNull(c.CompletedAt);
    }

    [Fact]
    public async Task UpdateProgress_WrongAssignee_ReturnsConflict()
    {
        var (svc, repo, _, _) = NewService();
        var c = OpenContract();
        c.Status = ContractStatus.Assigned;
        c.AssigneeActorId = Guid.NewGuid();
        repo.Contracts.Add(c);

        var result = await svc.UpdateProgressAsync(c.Id, new UpdateContractProgressRequest(Guid.NewGuid(), 3));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateProgress_Assignee_SetsProgress()
    {
        var (svc, repo, _, _) = NewService();
        var actor = Guid.NewGuid();
        var c = OpenContract();
        c.Status = ContractStatus.Assigned;
        c.AssigneeActorId = actor;
        repo.Contracts.Add(c);

        var result = await svc.UpdateProgressAsync(c.Id, new UpdateContractProgressRequest(actor, 7));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, c.ProgressCount);
    }

    // --- JSON objective fields --------------------------------------------

    [Fact]
    public async Task GetByRegion_ParsesFailureConditionsAndReputation()
    {
        var (svc, repo, _, _) = NewService();
        repo.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(),
            Status = ContractStatus.Open,
            ConditionsJson = "{\"targetCount\":3,\"minReputation\":5,\"timeLimitSeconds\":600,\"failOn\":[\"timeout\",\"death\"],\"issuer\":\"Guildmaster\"}",
            RewardJson = "{\"currency\":120,\"reputation\":8}",
        });

        var result = await svc.GetByRegionAsync(Guid.NewGuid());
        var r = Assert.Single(result.Value!);

        Assert.Equal(3, r.TargetCount);
        Assert.Equal(120, r.RewardCurrency);
        Assert.Equal(8, r.RewardReputation);
        Assert.Equal(5, r.MinReputation);
        Assert.Equal(600, r.TimeLimitSeconds);
        Assert.True(r.FailOnTimeout);
        Assert.True(r.FailOnDeath);
        Assert.Equal("Guildmaster", r.Issuer);
    }

    [Fact]
    public async Task GetByRegion_MalformedJson_FallsBackWithoutThrowing()
    {
        var (svc, repo, _, _) = NewService();
        repo.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(),
            Status = ContractStatus.Open,
            ConditionsJson = "not json at all",
            RewardJson = "",
        });

        var result = await svc.GetByRegionAsync(Guid.NewGuid());
        var r = Assert.Single(result.Value!);

        Assert.Equal(1, r.TargetCount);      // fallback
        Assert.Equal(0, r.RewardCurrency);   // fallback
        Assert.False(r.FailOnTimeout);       // absent → never fails
        Assert.Null(r.Target);
        Assert.Equal("", r.Issuer);
    }
}
