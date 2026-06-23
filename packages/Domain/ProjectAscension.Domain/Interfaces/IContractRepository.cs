#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectAscension.Domain.Entities;
namespace ProjectAscension.Domain.Interfaces
{
    public interface IContractRepository
    {
        Task<IReadOnlyList<Contract>> GetByRegionAsync(Guid regionId, CancellationToken ct = default);
        Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task UpdateAsync(Contract contract, CancellationToken ct = default);
    }
}
