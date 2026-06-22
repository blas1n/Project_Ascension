using ProjectAscension.Contracts.Enums;
namespace ProjectAscension.Contracts.Responses;
public record ContractResponse(Guid Id, ContractKind Kind, string Title, string Description, ContractStatus Status, string RewardJson);
