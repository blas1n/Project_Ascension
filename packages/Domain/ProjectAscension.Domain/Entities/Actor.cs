using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Actor
{
    public Guid Id { get; set; }
    public ActorType Type { get; set; }
    public Guid? CharacterId { get; set; }
    public Guid? NpcId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Character? Character { get; set; }
    public NPC? Npc { get; set; }
}

public enum ActorType { Player, NPC }
