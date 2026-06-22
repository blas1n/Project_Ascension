using MessagePack;
namespace ProjectAscension.Contracts.GameMessages;

[MessagePackObject]
public record PlayerInputMessage(
    [property: Key(0)] Guid ActorId,
    [property: Key(1)] float MoveX,
    [property: Key(2)] float MoveZ,
    [property: Key(3)] bool Jump,
    [property: Key(4)] bool Dodge,
    [property: Key(5)] bool Attack,
    [property: Key(6)] int Sequence
);
