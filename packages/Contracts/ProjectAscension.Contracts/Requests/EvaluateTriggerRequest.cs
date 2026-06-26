#nullable enable
using System;
using System.Collections.Generic;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Asks the rule engine to score a behavior signature and, if it crosses
    /// the significance threshold, fire a discovery (ADR 0002 core 4 — a function, not
    /// a fixed catalog). The signature signals come from the client's accumulated
    /// behavior; the engine owns the scoring and the resulting rarity.</summary>
    public record EvaluateTriggerRequest(
        Guid ActorId,
        Guid RegionId,
        DiscoveryType Type,
        string Theme,
        IReadOnlyList<string> ContextTags,
        string PrimaryBehavior,
        int Frequency,
        int Persistence,
        int Difficulty,
        int Combination);
}
