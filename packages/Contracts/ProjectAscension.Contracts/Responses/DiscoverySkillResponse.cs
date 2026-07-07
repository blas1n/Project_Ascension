#nullable enable
using System;
using System.Collections.Generic;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>A discovery's content state: Pending until the AI composes it, then
    /// the frozen skill (name/description/primitives/power cost).</summary>
    public record DiscoverySkillResponse(
        Guid DiscoveryId,
        DiscoveryContentStatus Status,
        string? Name,
        string? Description,
        int? PowerCost,
        IReadOnlyList<string> Primitives,
        string? Manifestation,
        IReadOnlyList<string> ContextTags,
        IReadOnlyList<string> Behaviors,
        IReadOnlyList<string> InvocationCombo,
        string Delivery,
        string? EffectGraph = null);
}
