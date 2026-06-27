#nullable enable
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>Reads the runtime-editable combat balance data. Loaded fresh so DB edits
    /// apply without a restart.</summary>
    public interface ICombatTuningRepository
    {
        Task<CombatTuningSettings?> GetSettingsAsync(CancellationToken ct = default);
    }
}
