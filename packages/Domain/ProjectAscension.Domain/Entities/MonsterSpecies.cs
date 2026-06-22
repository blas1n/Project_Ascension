using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class MonsterSpecies
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MonsterTier Tier { get; set; }
    public string DropsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
}
