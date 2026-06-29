#nullable enable
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>Reads and persists the player's saved progress.</summary>
    public interface IPlayerProfileRepository
    {
        Task<PlayerProfile?> GetAsync(CancellationToken ct = default);
        Task UpdateAsync(PlayerProfile profile, CancellationToken ct = default);
    }
}
