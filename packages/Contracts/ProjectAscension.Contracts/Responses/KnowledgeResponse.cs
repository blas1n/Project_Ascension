#nullable enable
using System;

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>An owned knowledge asset — links an actor (owner) to a discovery.
    /// The skill content is fetched via the discovery's skill endpoint.</summary>
    public record KnowledgeResponse(Guid Id, Guid DiscoveryId, Guid OwnerActorId, DateTime CreatedAt);
}
