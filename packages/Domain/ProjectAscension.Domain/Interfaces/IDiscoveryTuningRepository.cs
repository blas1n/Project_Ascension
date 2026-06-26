#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>Reads the runtime-editable discovery balance data. Loaded fresh per
    /// evaluation so DB edits apply without a restart.</summary>
    public interface IDiscoveryTuningRepository
    {
        Task<DiscoveryTuningSettings?> GetSettingsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<BehaviorWeight>> GetBehaviorWeightsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<FactorWeight>> GetFactorWeightsAsync(CancellationToken ct = default);
    }
}
