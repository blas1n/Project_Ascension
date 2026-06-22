using MessagePack;
namespace ProjectAscension.Contracts.GameMessages;

[MessagePackObject]
public record GameEventMessage(
    [property: Key(0)] string EventType,
    [property: Key(1)] Guid ActorId,
    [property: Key(2)] string PayloadJson
);
