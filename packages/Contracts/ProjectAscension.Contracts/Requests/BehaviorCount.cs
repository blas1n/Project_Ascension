#nullable enable

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>How many times the player performed a behavior (e.g. "Jump", 50).
    /// A reported observation; the server owns its difficulty weight and scoring.</summary>
    public record BehaviorCount(string Behavior, int Count);
}
