#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    /// <summary>Records and reads the discovery graph (parent → child edges).</summary>
    public interface IDiscoveryLineageRepository
    {
        Task AddEdgesAsync(IEnumerable<DiscoveryLineage> edges, CancellationToken ct = default);
        Task<IReadOnlyList<DiscoveryLineage>> GetByChildAsync(Guid childDiscoveryId, CancellationToken ct = default);
    }
}
