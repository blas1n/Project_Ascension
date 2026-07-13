using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Tests;

/// <summary>Character creation (fresh-player identity, BUG 1): the client must never assume an
/// actor id — the server mints the Character + its Actor atomically, and everything downstream
/// (discovery, contracts, knowledge) keys off the actor id this returns (ADR 0014).</summary>
public class CharacterServiceTests
{
    private sealed class FakeCharacterRepo : ICharacterRepository
    {
        public List<Character> Characters { get; } = new();
        public List<Actor> Actors { get; } = new();

        public Task<Character?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Characters.FirstOrDefault(c => c.Id == id));

        public Task<Actor?> GetActorByCharacterIdAsync(Guid characterId, CancellationToken ct = default)
            => Task.FromResult(Actors.FirstOrDefault(a => a.CharacterId == characterId));

        public Task<bool> ActorExistsAsync(Guid actorId, CancellationToken ct = default)
            => Task.FromResult(Actors.Any(a => a.Id == actorId));

        public Task CreateAsync(Character character, Actor actor, CancellationToken ct = default)
        {
            Characters.Add(character);
            Actors.Add(actor);
            return Task.CompletedTask;
        }
    }

    private static (CharacterService svc, FakeCharacterRepo repo) NewService()
    {
        var repo = new FakeCharacterRepo();
        return (new CharacterService(repo), repo);
    }

    [Fact]
    public async Task Create_YieldsAUsableActorId()
    {
        var (svc, repo) = NewService();

        var result = await svc.CreateAsync(new CreateCharacterRequest("Ash"));

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.ActorId);
        Assert.Equal("Ash", result.Value.Name);
        // The actor this just minted is immediately usable — the exact check evaluate/trigger
        // run before touching the discovery tables (never a foreign-key 500 for a fresh player).
        Assert.True(await svc.ActorExistsAsync(result.Value.ActorId));
    }

    [Fact]
    public async Task Create_PersistsBothTheCharacterAndItsActor_Atomically()
    {
        var (svc, repo) = NewService();

        var result = await svc.CreateAsync(new CreateCharacterRequest("Tide"));

        var character = Assert.Single(repo.Characters);
        var actor = Assert.Single(repo.Actors);
        Assert.Equal(character.Id, actor.CharacterId);
        Assert.Equal(result.Value!.Id, character.Id);
        Assert.Equal(result.Value.ActorId, actor.Id);
    }

    [Fact]
    public async Task Create_BlankName_ReturnsInvalid()
    {
        var (svc, repo) = NewService();

        var result = await svc.CreateAsync(new CreateCharacterRequest("   "));

        Assert.False(result.IsSuccess);
        Assert.Empty(repo.Characters);
        Assert.Empty(repo.Actors);
    }

    [Fact]
    public async Task ActorExists_UnknownActor_ReturnsFalse()
    {
        var (svc, _) = NewService();

        Assert.False(await svc.ActorExistsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetById_UnknownCharacter_ReturnsNotFound()
    {
        var (svc, _) = NewService();

        var result = await svc.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }
}
