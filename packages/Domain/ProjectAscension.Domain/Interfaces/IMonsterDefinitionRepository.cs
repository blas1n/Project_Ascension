#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>Reads the runtime-editable monster stat definitions. Loaded fresh so DB
    /// edits apply without a restart.</summary>
    public interface IMonsterDefinitionRepository
    {
        Task<IReadOnlyList<MonsterDefinition>> GetAllAsync(CancellationToken ct = default);
    }
}
