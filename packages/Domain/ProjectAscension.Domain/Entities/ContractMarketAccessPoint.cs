namespace ProjectAscension.Domain.Entities;

public class ContractMarketAccessPoint
{
    public Guid Id { get; set; }
    public Guid RegionId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Region? Region { get; set; }
}
