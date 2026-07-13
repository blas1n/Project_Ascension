using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Controllers;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Tests;

/// <summary>BUG 1 (worst, fresh-start): evaluate used to 500 for a fresh player because
/// Discoveries.DiscovererActorId FK's to Actors, and a brand-new database (or a client that never
/// created a character) has no Actor row. The controller must reject an unknown actor with a clear
/// 4xx before ever reaching the composition service — a missing actor is a client error, not a
/// server crash.</summary>
public class DiscoveriesControllerTests
{
    private sealed class FakeCharacterService : ICharacterService
    {
        public HashSet<Guid> KnownActors { get; } = new();

        public Task<Result<CharacterResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<Result<CharacterResponse>> CreateAsync(CreateCharacterRequest request, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<bool> ActorExistsAsync(Guid actorId, CancellationToken ct = default)
            => Task.FromResult(KnownActors.Contains(actorId));
    }

    private sealed class FakeDiscoveryService : IDiscoveryService
    {
        public Task<Result<DiscoveryResponse>> RecordAsync(RecordDiscoveryRequest request, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<Result<IReadOnlyList<DiscoveryResponse>>> GetByActorAsync(Guid actorId, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeTuningProvider : IDiscoveryTuningProvider
    {
        public Task<ProjectAscension.SkillForge.DiscoveryTuning> GetAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeSkillCompositionService : ISkillCompositionService
    {
        public int EvaluateCalls { get; private set; }
        public int TriggerCalls { get; private set; }
        public EvaluateTriggerResponse EvaluateResponse { get; set; } = new(true, 99, Guid.NewGuid());

        public Task<Guid> TriggerAsync(TriggerDiscoveryRequest request, CancellationToken ct = default)
        {
            TriggerCalls++;
            return Task.FromResult(Guid.NewGuid());
        }

        public Task<EvaluateTriggerResponse> EvaluateAndTriggerAsync(EvaluateTriggerRequest request, CancellationToken ct = default)
        {
            EvaluateCalls++;
            return Task.FromResult(EvaluateResponse);
        }

        public Task<DiscoverySkillResponse?> GetByDiscoveryAsync(Guid discoveryId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<DiscoveryLineageResponse> GetLineageAsync(Guid discoveryId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task ComposePendingAsync(int batchSize, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private static (DiscoveriesController controller, FakeCharacterService characters, FakeSkillCompositionService composition) NewController()
    {
        var characters = new FakeCharacterService();
        var composition = new FakeSkillCompositionService();
        var controller = new DiscoveriesController(new FakeDiscoveryService(), composition, new FakeTuningProvider(), characters);
        return (controller, characters, composition);
    }

    private static EvaluateTriggerRequest EvalRequest(Guid actorId) => new(
        actorId, Guid.NewGuid(), DiscoveryType.Skill, "a theme",
        Array.Empty<string>(), "Projectile", Array.Empty<BehaviorCount>(), Persistence: 1);

    [Fact]
    public async Task Evaluate_UnknownActor_ReturnsClientError_NotServerCrash()
    {
        var (controller, _, composition) = NewController();

        var result = await controller.Evaluate(EvalRequest(Guid.NewGuid()), CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<Error>(bad.Value);
        Assert.Equal(0, composition.EvaluateCalls); // never reached the FK-violating insert path
    }

    [Fact]
    public async Task Evaluate_ActorCreated_Succeeds()
    {
        var (controller, characters, composition) = NewController();
        var actorId = Guid.NewGuid();
        characters.KnownActors.Add(actorId); // as if CharactersController.Create just ran

        var result = await controller.Evaluate(EvalRequest(actorId), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(composition.EvaluateResponse, ok.Value);
        Assert.Equal(1, composition.EvaluateCalls);
    }

    [Fact]
    public async Task Trigger_UnknownActor_ReturnsClientError_NotServerCrash()
    {
        var (controller, _, composition) = NewController();
        var request = new TriggerDiscoveryRequest(
            Guid.NewGuid(), Guid.NewGuid(), DiscoveryType.Skill, "a theme",
            Array.Empty<string>(), "Projectile", "Common");

        var result = await controller.Trigger(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, composition.TriggerCalls);
    }
}
