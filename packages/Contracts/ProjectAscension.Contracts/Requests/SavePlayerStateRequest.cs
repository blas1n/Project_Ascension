#nullable enable
using ProjectAscension.Contracts.Responses;

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Persist the player's current progress.</summary>
    public record SavePlayerStateRequest(int Currency, int Reputation, ResourceCount[] Resources, string[] SoldKnowledge);
}
