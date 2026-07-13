#nullable enable
using System;

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Report a contract failure. Only the INTENT (who + why, e.g. "died" /
    /// "expired") — the server reads the contract's own stored reward terms and computes the
    /// reputation penalty itself via ContractRules.ReputationPenalty; the client never sends
    /// (or decides) a number (ADR 0014).</summary>
    public record FailContractRequest(Guid ActorId, string Reason);
}
