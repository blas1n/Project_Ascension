namespace ProjectAscension.GameSimulation.Player
{
    public record PlayerInput(
        float MoveX,
        float MoveZ,
        bool Jump,
        bool Dodge,
        bool Attack,
        int Sequence
    );
}
