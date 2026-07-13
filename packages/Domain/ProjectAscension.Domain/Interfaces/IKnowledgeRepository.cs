#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    public interface IKnowledgeRepository
    {
        Task AddAsync(Knowledge knowledge, CancellationToken ct = default);
        Task<IReadOnlyList<Knowledge>> GetByOwnerAsync(Guid ownerActorId, CancellationToken ct = default);
        Task<Knowledge?> GetByDiscoveryIdAsync(Guid discoveryId, CancellationToken ct = default);
        Task UpdateAsync(Knowledge knowledge, CancellationToken ct = default);
    }
}
