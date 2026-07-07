using System.Numerics;

namespace ProjectAscension.GameSimulation.Player
{
    /// <summary>
    /// Pure, deterministic player movement step. Runs identically on the server
    /// (authority) and the Unity client (prediction). Input is expressed in world
    /// XZ; the client converts camera-relative input before calling this.
    /// </summary>
    public class PlayerSimulation
    {
        public PlayerState ApplyInput(PlayerState state, PlayerInput input, float deltaTime)
            => ApplyInput(state, input, deltaTime, MovementSettings.Default);

        public PlayerState ApplyInput(PlayerState state, PlayerInput input, float deltaTime, MovementSettings settings)
        {
            var velocity = state.Velocity;
            var dodgeVelocity = state.DodgeVelocity;
            var dodgeTimeRemaining = state.DodgeTimeRemaining;

            // Begin a dodge: only when grounded and not already dodging.
            if (input.Dodge && state.IsGrounded && dodgeTimeRemaining <= 0f)
            {
                var dir = new Vector3(input.MoveX, 0f, input.MoveZ);
                dir = dir.LengthSquared() > 0.0001f
                    ? Vector3.Normalize(dir)
                    : new Vector3(0f, 0f, 1f); // no input → dash forward (world +Z)

                dodgeVelocity = dir * settings.DodgeSpeed;
                dodgeTimeRemaining = settings.DodgeDuration;
            }

            // Horizontal movement: locked to the dodge vector while dodging.
            // (Construct new Vector3 values rather than `with` — `with` on structs
            // is C# 10, but this is shared with the Unity client which is C# 9.)
            if (dodgeTimeRemaining > 0f)
            {
                velocity = new Vector3(dodgeVelocity.X, velocity.Y, dodgeVelocity.Z);
                dodgeTimeRemaining -= deltaTime;
            }
            else
            {
                velocity = new Vector3(input.MoveX * settings.MoveSpeed, velocity.Y, input.MoveZ * settings.MoveSpeed);
            }

            // Wall-climb (ADR 0007): a discovered OnWallContact skill lets the player scale a wall.
            // While airborne against a wall and holding jump, ascend instead of falling; clinging
            // also refreshes the jump so a wall-jump is available. Overrides the normal jump +
            // gravity for the tick — no bespoke mechanic, just the graph's trigger acting.
            bool wallClimbing = settings.WallClimb && !state.IsGrounded && input.TouchingWall && input.Jump;

            // Jump: from the ground, or an EXTRA air jump (double jump) — but only after a
            // ground jump (JumpsUsed >= 1) and within the discovered-skill allowance. Walking
            // off a ledge (JumpsUsed 0, airborne) does not grant a free air jump.
            int jumpsUsed = state.JumpsUsed;
            bool canJump = !wallClimbing && input.Jump &&
                (state.IsGrounded || (jumpsUsed >= 1 && jumpsUsed < 1 + settings.ExtraJumps));
            if (canJump)
            {
                velocity = new Vector3(velocity.X, settings.JumpVelocity, velocity.Z);
                jumpsUsed = state.IsGrounded ? 1 : jumpsUsed + 1;
            }

            if (wallClimbing)
            {
                velocity = new Vector3(velocity.X, settings.WallClimbSpeed, velocity.Z);
                jumpsUsed = 0; // clinging refreshes the jump (wall-jump)
            }
            // Gravity (only when airborne and not clinging to a wall)
            else if (!state.IsGrounded)
            {
                velocity = new Vector3(velocity.X, velocity.Y - settings.Gravity * deltaTime, velocity.Z);
            }

            // Position update
            var position = state.Position + velocity * deltaTime;

            // Ground detection
            bool isGrounded = position.Y <= settings.GroundY;
            if (isGrounded)
            {
                position = new Vector3(position.X, settings.GroundY, position.Z);
                velocity = new Vector3(velocity.X, 0f, velocity.Z);
                jumpsUsed = 0; // landed — refresh jumps
            }

            return state with
            {
                Position = position,
                Velocity = velocity,
                IsGrounded = isGrounded,
                InputSequence = input.Sequence,
                DodgeVelocity = dodgeVelocity,
                DodgeTimeRemaining = dodgeTimeRemaining < 0f ? 0f : dodgeTimeRemaining,
                JumpsUsed = jumpsUsed
            };
        }
    }
}
