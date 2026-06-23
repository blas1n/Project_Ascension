#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;
namespace ProjectAscension.Domain.Interfaces
{
    public interface IDiscoveryRepository
    {
        Task<IReadOnlyList<Discovery>> GetByActorAsync(Guid actorId, CancellationToken ct = default);
        Task AddAsync(Discovery discovery, CancellationToken ct = default);
        Task<DiscoveryProgress?> GetProgressAsync(Guid actorId, Guid candidateId, CancellationToken ct = default);
        Task UpsertProgressAsync(DiscoveryProgress progress, CancellationToken ct = default);
    }
}
