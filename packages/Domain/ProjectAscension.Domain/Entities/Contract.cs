using ProjectAscension.Domain.Enums;
namespace ProjectAscension.Domain.Entities;

public class Contract
{
    public Guid Id { get; set; }
    public ContractKind Kind { get; set; }
    public ContractPurpose Purpose { get; set; }
    public Guid? IssuerActorId { get; set; }
    public Guid? AssigneeActorId { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Open;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RewardJson { get; set; } = "{}";
    public string ConditionsJson { get; set; } = "{}";
    public bool DelegationAllowed { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int ProgressCount { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
