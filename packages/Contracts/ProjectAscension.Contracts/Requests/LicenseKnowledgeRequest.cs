#nullable enable
using System;

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Sell a license for a discovered skill's knowledge — a license may be sold
    /// ONCE per discovery (server-enforced, ADR 0014). Only the INTENT (which discovery); the
    /// server derives price/reputation from the skill's own composed effect graph.</summary>
    public record LicenseKnowledgeRequest(Guid ActorId, Guid DiscoveryId);
}
