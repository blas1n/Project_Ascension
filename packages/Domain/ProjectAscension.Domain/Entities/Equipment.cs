using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Equipment
{
    public Guid ItemId { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public SlotType SlotType { get; set; }

    public Item? Item { get; set; }
}
