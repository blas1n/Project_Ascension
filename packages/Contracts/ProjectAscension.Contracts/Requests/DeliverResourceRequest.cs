#nullable enable

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Deliver a resource to the frontier outpost — matures the matching
    /// infrastructure track.</summary>
    public record DeliverResourceRequest(string ItemKey, int Amount);
}
