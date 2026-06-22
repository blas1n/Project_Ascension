using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Item
{
    public Guid Id { get; set; }
    public ItemType Type { get; set; }
    public Guid TemplateId { get; set; }
    public Guid? OwnerActorId { get; set; }
    public Guid? CurrentRegionId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}
