#nullable enable
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>Reads the runtime-editable player stat definition. Loaded fresh so DB
    /// edits apply without a restart.</summary>
    public interface IPlayerDefinitionRepository
    {
        Task<PlayerDefinition?> GetAsync(CancellationToken ct = default);
    }
}
