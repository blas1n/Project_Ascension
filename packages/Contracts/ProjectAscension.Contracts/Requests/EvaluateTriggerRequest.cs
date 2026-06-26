#nullable enable
using System;
using System.Collections.Generic;
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Asks the rule engine to score a behavior signature and, if it crosses
    /// the significance threshold, fire a discovery (ADR 0002 core 4 — a function, not
    /// a fixed catalog). <see cref="Behaviors"/> are the reported per-behavior counts
    /// and <see cref="Persistence"/> how sustained the pattern was; the engine owns
    /// the weights, scoring, rarity, and budget. <see cref="PrimaryBehavior"/> seeds
    /// the AI composition's primary effect.</summary>
    public record EvaluateTriggerRequest(
        Guid ActorId,
        Guid RegionId,
        DiscoveryType Type,
        string Theme,
        IReadOnlyList<string> ContextTags,
        string PrimaryBehavior,
        IReadOnlyList<BehaviorCount> Behaviors,
        int Persistence);
}
