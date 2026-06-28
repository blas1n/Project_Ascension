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
        Task AddAsync(Contract contract, CancellationToken ct = default);
        Task UpdateAsync(Contract contract, CancellationToken ct = default);

        /// <summary>The runtime-editable reward calibration for player-issued contracts
        /// (null falls back to defaults).</summary>
        Task<ContractRewardTuning?> GetRewardTuningAsync(CancellationToken ct = default);
    }
}
