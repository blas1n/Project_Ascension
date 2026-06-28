#nullable enable
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>Reads and persists the frontier outpost's development.</summary>
    public interface ISettlementRepository
    {
        Task<Settlement?> GetAsync(CancellationToken ct = default);
        Task UpdateAsync(Settlement settlement, CancellationToken ct = default);
    }
}
