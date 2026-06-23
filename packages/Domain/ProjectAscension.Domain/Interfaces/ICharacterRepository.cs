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
    }
}
