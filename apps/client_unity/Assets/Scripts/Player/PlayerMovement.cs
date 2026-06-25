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
        private bool _dodgeQueued;

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
        public void QueueDodge() => _dodgeQueued = true;

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

            // Dodge with no directional input dashes toward where the player is
            // facing (horizontal forward), not a fixed world axis. Only matters on
            // the dodge-start frame; the simulation locks the dash vector after that.
            if (_dodgeQueued && world.sqrMagnitude < 0.0001f)
                world = forward;

            var input = new PlayerInput(
                MoveX: world.x,
                MoveZ: world.z,
                Jump: _jumpQueued,
                Dodge: _dodgeQueued,
                Attack: false,
                Sequence: _state.InputSequence + 1);

            // Detect actual execution against the pre-tick state, mirroring the
            // simulation's gates, so behavior events fire on real jumps/dodges —
            // not on every input press (airborne spam, dodge on cooldown).
            bool jumpExecuted = _jumpQueued && _state.IsGrounded;
            bool dodgeExecuted = _dodgeQueued && _state.IsGrounded && _state.DodgeTimeRemaining <= 0f;

            _state = _simulation.ApplyInput(_state, input, deltaTime, _data.ToMovementSettings());
            _jumpQueued = false;
            _dodgeQueued = false;

            if (jumpExecuted) GameplayEvents.RaiseJumped();
            if (dodgeExecuted) GameplayEvents.RaiseDodged();

            // Visually sync the CharacterController to the predicted position.
            var target = new Vector3(_state.Position.X, _state.Position.Y, _state.Position.Z);
            _controller.Move(target - _body.position);
        }
    }
}
