using MessagePack;
namespace ProjectAscension.Contracts.GameMessages;

[MessagePackObject]
public record WorldStateMessage(
    [property: Key(0)] long Tick,
    [property: Key(1)] EntitySnapshot[] Entities
);

[MessagePackObject]
public record EntitySnapshot(
    [property: Key(0)] Guid ActorId,
    [property: Key(1)] float PosX,
    [property: Key(2)] float PosY,
    [property: Key(3)] float PosZ,
    [property: Key(4)] float VelX,
    [property: Key(5)] float VelY,
    [property: Key(6)] float VelZ
);
