using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Requests;

public record RecordDiscoveryRequest(Guid ActorId, Guid RegionId, DiscoveryType Type, string Title, string Description);
