using System.Numerics;

namespace ProjectAscension.GameSimulation.Player;

public class PlayerSimulation
{
    private const float MoveSpeed = 5f;
    private const float JumpVelocity = 6f;
    private const float Gravity = 20f;
    private const float GroundY = 0f;

    public PlayerState ApplyInput(PlayerState state, PlayerInput input, float deltaTime)
    {
        var velocity = state.Velocity;

        // Horizontal movement
        velocity = velocity with
        {
            X = input.MoveX * MoveSpeed,
            Z = input.MoveZ * MoveSpeed
        };

        // Jump (only when grounded)
        if (input.Jump && state.IsGrounded)
            velocity = velocity with { Y = JumpVelocity };

        // Gravity (only when airborne)
        if (!state.IsGrounded)
            velocity = velocity with { Y = velocity.Y - Gravity * deltaTime };

        // Position update
        var position = state.Position + velocity * deltaTime;

        // Ground detection
        bool isGrounded = position.Y <= GroundY;
        if (isGrounded)
        {
            position = position with { Y = GroundY };
            velocity = velocity with { Y = 0f };
        }

        return state with
        {
            Position = position,
            Velocity = velocity,
            IsGrounded = isGrounded,
            InputSequence = input.Sequence
        };
    }
}
