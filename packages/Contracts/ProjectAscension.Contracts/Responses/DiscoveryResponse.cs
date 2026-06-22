using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Responses;

public record DiscoveryResponse(Guid Id, DiscoveryType Type, string Title, string Description, DateTime DiscoveredAt);
