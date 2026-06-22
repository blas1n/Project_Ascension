namespace ProjectAscension.Domain.Entities;

public class Loadout
{
    public Guid ActorId { get; set; }
    public Guid? LeftItemId { get; set; }
    public Guid? RightItemId { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Actor? Actor { get; set; }
}
