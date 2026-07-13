#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;
namespace ProjectAscension.Domain.Interfaces
{
    public interface ICharacterRepository
    {
        Task<Character?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Actor?> GetActorByCharacterIdAsync(Guid characterId, CancellationToken ct = default);

        /// <summary>Whether an Actor row exists for this id — the pre-flight check that keeps a
        /// missing/foreign actor a 4xx (a client error) instead of a foreign-key 500.</summary>
        Task<bool> ActorExistsAsync(Guid actorId, CancellationToken ct = default);

        /// <summary>Creates the Character and its Actor atomically — character creation is the
        /// only place an actor id is minted (a fresh database has none until this runs).</summary>
        Task CreateAsync(Character character, Actor actor, CancellationToken ct = default);
    }
}
