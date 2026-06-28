#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>Reads the runtime-editable shop item definitions.</summary>
    public interface IItemDefinitionRepository
    {
        Task<IReadOnlyList<ItemDefinition>> GetAllAsync(CancellationToken ct = default);
    }
}
