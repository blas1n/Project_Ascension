#nullable enable
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>The runtime-editable knowledge-license rate (null falls back to defaults).</summary>
    public interface IEconomyTuningRepository
    {
        Task<EconomyTuning?> GetAsync(CancellationToken ct = default);
    }
}
