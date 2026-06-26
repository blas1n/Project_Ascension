#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;

namespace ProjectAscension.Domain.Interfaces
{
    public interface IDiscoverySkillRepository
    {
        Task AddAsync(DiscoverySkill skill, CancellationToken ct = default);
        Task<DiscoverySkill?> GetByDiscoveryIdAsync(Guid discoveryId, CancellationToken ct = default);
        Task<DiscoverySkill?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);
        Task<IReadOnlyList<DiscoverySkill>> GetPendingAsync(int limit, CancellationToken ct = default);
        Task UpdateAsync(DiscoverySkill skill, CancellationToken ct = default);
    }
}
