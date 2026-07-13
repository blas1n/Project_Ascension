using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Player;
using SimVector3 = System.Numerics.Vector3;

namespace ProjectAscension.Player
{
    /// <summary>
    /// Client-side movement. The authoritative step is the shared
    /// <see cref="PlayerSimulation"/> (so server and client agree); the
    /// CharacterController is only moved to match the predicted position for
    /// rendering and visual wall collisions. Server reconciliation is a later phase.
    /// </summary>
    public sealed class PlayerMovement
    {
        private readonly PlayerSimulation _simulation;
        private readonly PlayerData _data;

        private CharacterController _controller;
        private Transform _body;
        private PlayerState _state;
        private bool _jumpQueued;
        private bool _touchingWall; // set from the controller's side collisions last tick (wall-climb)

        public PlayerMovement(PlayerSimulation simulation, PlayerData data)
        {
            _simulation = simulation;
            _data = data;
        }

        public void Initialize(CharacterController controller, Transform body)
        {
            _controller = controller;
            _body = body;
            var p = body.position;
            _state = new PlayerState(new SimVector3(p.x, p.y, p.z), SimVector3.Zero, IsGrounded: true, InputSequence: 0);
        }

        public void QueueJump() => _jumpQueued = true;

        /// <summary>Hard-set position (respawn) and clear velocity in the simulation.</summary>
        public void Teleport(Vector3 position)
        {
            if (_controller == null) return;

            _controller.enabled = false;
            _body.position = position;
            _controller.enabled = true;

            _state = _state with
            {
                Position = new SimVector3(position.x, position.y, position.z),
                Velocity = SimVector3.Zero
            };
        }

        public void Tick(Vector2 moveInput, float deltaTime)
        {
            if (_controller == null) return;

            // Convert local stick input into a world-space direction using the
            // body's current yaw (set by PlayerCamera).
            Vector3 forward = _body.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = _body.right; right.y = 0f; right.Normalize();
            Vector3 world = forward * moveInput.y + right * moveInput.x;
            if (world.sqrMagnitude > 1f) world.Normalize();

            var input = new PlayerInput(
                MoveX: world.x,
                MoveZ: world.z,
                Jump: _jumpQueued,
                Attack: false,
                Sequence: _state.InputSequence + 1,
                TouchingWall: _touchingWall);

            // Detect actual execution against the pre-tick state, mirroring the
            // simulation's gates, so behavior events fire on real jumps —
            // not on every input press (airborne spam).
            bool jumpExecuted = _jumpQueued && _state.IsGrounded;

            _state = _simulation.ApplyInput(_state, input, deltaTime, _data.ToMovementSettings());
            _jumpQueued = false;

            if (jumpExecuted) GameplayEvents.RaiseJumped();

            // Move the CharacterController toward the simulation's predicted position. The
            // sim only knows the ground plane, so it ignores obstacles; the controller's
            // sweep does collide with them.
            var target = new Vector3(_state.Position.X, _state.Position.Y, _state.Position.Z);
            var flags = _controller.Move(target - _body.position);

            // The controller's sweep reports side contact — feed it to next tick's input so a
            // discovered wall-climb skill can act against real level geometry (ADR 0007 Phase 2c).
            _touchingWall = (flags & CollisionFlags.Sides) != 0;

            // Adopt the collision-resolved HORIZONTAL position back into the sim, so a wall
            // that stopped the controller also stops the sim — otherwise the obstacle-unaware
            // sim keeps advancing past the wall and the body drifts through it. Vertical
            // (jump/gravity/ground) stays the sim's authority. Server reconciliation is a
            // later phase; until then the client resolves obstacle collision locally.
            var resolved = _body.position;
            _state = _state with { Position = new SimVector3(resolved.x, _state.Position.Y, resolved.z) };
        }
    }
}
