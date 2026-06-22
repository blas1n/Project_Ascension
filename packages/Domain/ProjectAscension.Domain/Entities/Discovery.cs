using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Discovery
{
    public Guid Id { get; set; }
    public DiscoveryType Type { get; set; }
    public Guid DiscovererActorId { get; set; }
    public Guid RegionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DiscoveredAt { get; set; }

    public Actor? Discoverer { get; set; }
    public Region? Region { get; set; }
}
