using System.Numerics;
using ProjectAscension.GameSimulation.Player;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Player;

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
}
