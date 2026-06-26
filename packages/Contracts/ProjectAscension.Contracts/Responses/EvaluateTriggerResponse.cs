#nullable enable
using System;

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>Result of scoring a behavior signature: whether a discovery fired, the
    /// raw significance score, and the new discovery id when it did.</summary>
    public record EvaluateTriggerResponse(bool Fired, int Score, Guid? DiscoveryId);
}
