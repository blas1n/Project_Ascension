#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    public record ResourceCount(string Key, int Count);

    /// <summary>The player's saved progress — currency, standing, materials, and licensed
    /// knowledge — restored on load.</summary>
    public record PlayerStateResponse(int Currency, int Reputation, ResourceCount[] Resources, string[] SoldKnowledge);
}
