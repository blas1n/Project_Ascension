namespace ProjectAscension.Contracts.Responses;

public record CharacterResponse(Guid Id, Guid ActorId, string Name, Guid CurrentRegionId, string Status);
