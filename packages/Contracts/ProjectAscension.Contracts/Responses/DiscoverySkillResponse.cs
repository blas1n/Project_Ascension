#nullable enable
using System;
using System.Collections.Generic;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>A discovery's content state: Pending until the AI composes it, then
    /// the frozen skill (name/description/effect graph/power cost).</summary>
    public record DiscoverySkillResponse(
        Guid DiscoveryId,
        DiscoveryContentStatus Status,
        string? Name,
        string? Description,
        int? PowerCost,
        string? Manifestation,
        IReadOnlyList<string> ContextTags,
        IReadOnlyList<string> Behaviors,
        string Delivery,
        string? EffectGraph = null,
        // Whether the owner has already sold this discovery's knowledge license (ADR 0014) —
        // server-authoritative truth so the client never offers to sell what can only 409.
        bool Licensed = false);
}
