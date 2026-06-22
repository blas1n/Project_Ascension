using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Region
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RegionType Type { get; set; }
    public Guid? ParentRegionId { get; set; }
    public int DangerLevel { get; set; }
    public string EnvironmentTagsJson { get; set; } = "[]";
}
