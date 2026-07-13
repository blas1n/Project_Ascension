#nullable enable
using System;

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Hand in a completed contract. Only the INTENT (who) — the server reads its own
    /// stored progress/reward and rejects if the contract isn't actually complete (ADR 0014).</summary>
    public record TurnInContractRequest(Guid ActorId);
}
