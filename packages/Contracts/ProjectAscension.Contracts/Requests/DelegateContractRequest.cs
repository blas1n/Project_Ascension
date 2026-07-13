#nullable enable
using System;

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Hand the active contract to a stub contractor instead of clearing it yourself.
    /// The server escrows the reward as the contractor's fee (ADR 0014).</summary>
    public record DelegateContractRequest(Guid ActorId);
}
