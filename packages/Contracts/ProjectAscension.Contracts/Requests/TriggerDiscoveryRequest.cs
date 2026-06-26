#nullable enable
using System;
using System.Collections.Generic;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Triggers a discovery: the rule engine fixes the fact instantly, the
    /// seed (theme/context/behavior/budget) is captured, and the AI composes the
    /// skill content asynchronously (ADR 0002).</summary>
    public record TriggerDiscoveryRequest(
        Guid ActorId,
        Guid RegionId,
        DiscoveryType Type,
        string Theme,
        IReadOnlyList<string> ContextTags,
        string PrimaryBehavior,
        string Rarity);
}
