namespace ProjectAscension.GameSimulation.Player
{
    public record PlayerInput(
        float MoveX,
        float MoveZ,
        bool Jump,
        bool Attack,
        int Sequence,
        bool TouchingWall = false // against a wall this tick — enables wall-climb (ADR 0007)
    );
}
