using System.Numerics;
using ProjectAscension.GameSimulation.Player;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Player
{
    public class PlayerSimulationTests
    {
        private readonly PlayerSimulation _sim = new();

        [Fact]
        public void ApplyInput_MoveForward_IncreasesZPosition()
        {
            var state = new PlayerState(Vector3.Zero, Vector3.Zero, IsGrounded: true, InputSequence: 0);
            var input = new PlayerInput(MoveX: 0f, MoveZ: 1f, Jump: false, Dodge: false, Attack: false, Sequence: 1);

            var next = _sim.ApplyInput(state, input, deltaTime: 0.016f);

            Assert.True(next.Position.Z > 0f);
        }

        [Fact]
        public void ApplyInput_JumpWhenGrounded_AppliesUpwardVelocity()
        {
            var state = new PlayerState(Vector3.Zero, Vector3.Zero, IsGrounded: true, InputSequence: 0);
            var input = new PlayerInput(MoveX: 0f, MoveZ: 0f, Jump: true, Dodge: false, Attack: false, Sequence: 1);

            var next = _sim.ApplyInput(state, input, deltaTime: 0.016f);

            Assert.True(next.Velocity.Y > 0f);
            Assert.False(next.IsGrounded);
            Assert.Equal(1, next.JumpsUsed);
        }

        [Fact]
        public void ApplyInput_DoubleJump_ReboostsWhenExtraJumpsGranted()
        {
            var settings = MovementSettings.Default with { ExtraJumps = 1 };
            // Airborne and falling, having used the ground jump — an extra jump is allowed once.
            var falling = new PlayerState(new Vector3(0, 5, 0), new Vector3(0, -3, 0),
                IsGrounded: false, InputSequence: 0, JumpsUsed: 1);
            var jump = new PlayerInput(0, 0, Jump: true, Dodge: false, Attack: false, Sequence: 1);

            var next = _sim.ApplyInput(falling, jump, deltaTime: 0.016f, settings);

            Assert.True(next.Velocity.Y > 0f);   // re-boosted upward from a fall
            Assert.Equal(2, next.JumpsUsed);
        }

        [Fact]
        public void ApplyInput_DoubleJump_DeniedWithoutExtraJumps()
        {
            // Same falling state, but the default has no extra jumps — the air jump does nothing.
            var falling = new PlayerState(new Vector3(0, 5, 0), new Vector3(0, -3, 0),
                IsGrounded: false, InputSequence: 0, JumpsUsed: 1);
            var jump = new PlayerInput(0, 0, Jump: true, Dodge: false, Attack: false, Sequence: 1);

            var next = _sim.ApplyInput(falling, jump, deltaTime: 0.016f); // default ExtraJumps = 0

            Assert.True(next.Velocity.Y < 0f); // still falling
        }

        [Fact]
        public void ApplyInput_WallClimb_AscendsWhenAgainstWallAndGranted()
        {
            var settings = MovementSettings.Default with { WallClimb = true, WallClimbSpeed = 4f };
            // Airborne, falling, pressed against a wall, holding jump — a discovered wall-climb.
            var falling = new PlayerState(new Vector3(0, 5, 0), new Vector3(0, -3, 0),
                IsGrounded: false, InputSequence: 0, JumpsUsed: 1);
            var climb = new PlayerInput(0, 0, Jump: true, Dodge: false, Attack: false, Sequence: 1, TouchingWall: true);

            var next = _sim.ApplyInput(falling, climb, deltaTime: 0.016f, settings);

            Assert.Equal(4f, next.Velocity.Y, precision: 3); // ascends at the climb speed, not falling
            Assert.Equal(0, next.JumpsUsed);                 // clinging refreshes the jump (wall-jump)
        }

        [Fact]
        public void ApplyInput_WallClimb_FallsWhenNotTouchingWall()
        {
            var settings = MovementSettings.Default with { WallClimb = true };
            var falling = new PlayerState(new Vector3(0, 5, 0), new Vector3(0, -3, 0),
                IsGrounded: false, InputSequence: 0, JumpsUsed: 1);
            var jump = new PlayerInput(0, 0, Jump: true, Dodge: false, Attack: false, Sequence: 1, TouchingWall: false);

            var next = _sim.ApplyInput(falling, jump, deltaTime: 0.016f, settings);

            Assert.True(next.Velocity.Y < 0f); // no wall → gravity, still falling
        }

        [Fact]
        public void ApplyInput_WallClimb_DeniedWithoutTheCapability()
        {
            // Touching a wall but no discovered wall-climb — the flag does nothing.
            var falling = new PlayerState(new Vector3(0, 5, 0), new Vector3(0, -3, 0),
                IsGrounded: false, InputSequence: 0, JumpsUsed: 1);
            var climb = new PlayerInput(0, 0, Jump: true, Dodge: false, Attack: false, Sequence: 1, TouchingWall: true);

            var next = _sim.ApplyInput(falling, climb, deltaTime: 0.016f); // default WallClimb = false

            Assert.True(next.Velocity.Y < 0f); // still falling
        }

        [Fact]
        public void ApplyInput_JumpWhenAirborne_NoAdditionalVelocity()
        {
            var airState = new PlayerState(new Vector3(0, 2, 0), new Vector3(0, 3, 0), IsGrounded: false, InputSequence: 0);
            var input = new PlayerInput(MoveX: 0f, MoveZ: 0f, Jump: true, Dodge: false, Attack: false, Sequence: 1);

            var next = _sim.ApplyInput(airState, input, deltaTime: 0.016f);

            Assert.True(next.Velocity.Y < 3f); // gravity applies, no extra jump
        }

        [Fact]
        public void ApplyInput_Gravity_PullsDownWhenAirborne()
        {
            var airState = new PlayerState(new Vector3(0, 5, 0), Vector3.Zero, IsGrounded: false, InputSequence: 0);
            var input = new PlayerInput(MoveX: 0f, MoveZ: 0f, Jump: false, Dodge: false, Attack: false, Sequence: 1);

            var next = _sim.ApplyInput(airState, input, deltaTime: 0.016f);

            Assert.True(next.Velocity.Y < 0f);
        }

        [Fact]
        public void ApplyInput_LandsOnGround_SetsIsGrounded()
        {
            var nearGround = new PlayerState(new Vector3(0, 0.01f, 0), new Vector3(0, -1f, 0), IsGrounded: false, InputSequence: 0);
            var input = new PlayerInput(MoveX: 0f, MoveZ: 0f, Jump: false, Dodge: false, Attack: false, Sequence: 1);

            var next = _sim.ApplyInput(nearGround, input, deltaTime: 0.016f);

            Assert.True(next.IsGrounded);
            Assert.Equal(0f, next.Position.Y);
        }

        [Fact]
        public void ApplyInput_DodgeWhenGrounded_AppliesDashFasterThanWalk()
        {
            var state = new PlayerState(Vector3.Zero, Vector3.Zero, IsGrounded: true, InputSequence: 0);
            var input = new PlayerInput(MoveX: 1f, MoveZ: 0f, Jump: false, Dodge: true, Attack: false, Sequence: 1);

            var next = _sim.ApplyInput(state, input, deltaTime: 0.016f);

            Assert.True(next.DodgeTimeRemaining > 0f);
            Assert.True(next.Velocity.X > MovementSettings.Default.MoveSpeed);
        }

        [Fact]
        public void ApplyInput_DodgeUsesSettingsSpeed()
        {
            var settings = MovementSettings.Default with { DodgeSpeed = 30f };
            var state = new PlayerState(Vector3.Zero, Vector3.Zero, IsGrounded: true, InputSequence: 0);
            var input = new PlayerInput(MoveX: 0f, MoveZ: 1f, Jump: false, Dodge: true, Attack: false, Sequence: 1);

            var next = _sim.ApplyInput(state, input, deltaTime: 0.016f, settings);

            Assert.Equal(30f, next.Velocity.Z, precision: 3);
        }

        // --- Dodge i-frames (a well-timed dodge negates the hit). ---

        [Fact]
        public void IsInvulnerable_HoldsForTheLeadingWindow_ThenTheRecoveryTailIsVulnerable()
        {
            // Duration 0.2, fraction 0.75 → invulnerable while remaining > 0.05 (the last 25% is exposed).
            Assert.True(PlayerSimulation.IsInvulnerable(dodgeTimeRemaining: 0.2f, dodgeDuration: 0.2f, iframeFraction: 0.75f));
            Assert.True(PlayerSimulation.IsInvulnerable(dodgeTimeRemaining: 0.06f, dodgeDuration: 0.2f, iframeFraction: 0.75f));
            Assert.False(PlayerSimulation.IsInvulnerable(dodgeTimeRemaining: 0.04f, dodgeDuration: 0.2f, iframeFraction: 0.75f)); // recovery tail
        }

        [Fact]
        public void IsInvulnerable_FalseWhenNotDodging()
        {
            Assert.False(PlayerSimulation.IsInvulnerable(dodgeTimeRemaining: 0f, dodgeDuration: 0.2f, iframeFraction: 0.75f));
            Assert.False(PlayerSimulation.IsInvulnerable(dodgeTimeRemaining: -1f, dodgeDuration: 0.2f, iframeFraction: 0.75f));
        }

        [Theory]
        [InlineData(0f)]   // no i-frames → never invulnerable, even mid-dodge
        [InlineData(1f)]   // full-window i-frames → invulnerable for the whole dodge
        public void IsInvulnerable_RespectsTheFractionExtremes(float fraction)
        {
            bool atStart = PlayerSimulation.IsInvulnerable(dodgeTimeRemaining: 0.2f, dodgeDuration: 0.2f, iframeFraction: fraction);
            bool nearEnd = PlayerSimulation.IsInvulnerable(dodgeTimeRemaining: 0.01f, dodgeDuration: 0.2f, iframeFraction: fraction);
            Assert.Equal(fraction > 0f, atStart);
            Assert.Equal(fraction >= 1f, nearEnd);
        }
    }
}
