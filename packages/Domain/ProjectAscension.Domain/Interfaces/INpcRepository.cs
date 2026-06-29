#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>Reads the city/world NPC roster.</summary>
    public interface INpcRepository
    {
        Task<IReadOnlyList<NPC>> GetAllAsync(CancellationToken ct = default);
    }
}
