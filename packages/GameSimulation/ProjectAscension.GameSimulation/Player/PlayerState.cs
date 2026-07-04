using System.Numerics;

namespace ProjectAscension.GameSimulation.Player
{
    public record PlayerState(
        Vector3 Position,
        Vector3 Velocity,
        bool IsGrounded,
        int InputSequence,
        Vector3 DodgeVelocity = default,
        float DodgeTimeRemaining = 0f,
        int JumpsUsed = 0 // jumps taken since leaving the ground (enables air jumps / double jump)
    );
}
