namespace ProjectAscension.GameSimulation.Player
{
    /// <summary>
    /// Authoritative movement tuning shared by the server simulation and the Unity
    /// client prediction. The client feeds these from a ScriptableObject so numbers
    /// are never hardcoded on the Unity side, while the server uses the same values
    /// to keep prediction and authority in sync.
    /// </summary>
    public record MovementSettings(
        float MoveSpeed = 5f,
        float JumpVelocity = 6f,
        float Gravity = 20f,
        float GroundY = 0f,
        float DodgeSpeed = 12f,
        float DodgeDuration = 0.2f,
        int ExtraJumps = 0 // air jumps beyond the ground jump (mobility passives — double jump)
    )
    {
        public static readonly MovementSettings Default = new();
    }
}
