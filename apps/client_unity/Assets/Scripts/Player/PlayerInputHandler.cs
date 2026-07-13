using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAscension.Player
{
    /// <summary>
    /// Thin wrapper over the New Input System "Player" action map. Publishes
    /// movement as an event and surfaces continuous Move/Look values for polling.
    /// Owned by VContainer; disposed when the scope ends.
    /// </summary>
    public sealed class PlayerInputHandler : IDisposable
    {
        private readonly InputActionMap _playerMap;
        private readonly InputAction _move;
        private readonly InputAction _look;
        private readonly InputAction _jump;
        private readonly InputAction _attack;
        private readonly InputAction _attackLeft;

        public event Action<Vector2> MoveInput;
        public event Action JumpPressed;

        /// <summary>Primary attack (right-hand weapon) — pressed / released (for charge).</summary>
        public event Action AttackPressed;
        public event Action AttackReleased;

        /// <summary>Secondary attack (left-hand weapon) — pressed / released (for charge).</summary>
        public event Action AttackLeftPressed;
        public event Action AttackLeftReleased;

        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 Look => _look.ReadValue<Vector2>();

        public PlayerInputHandler(InputActionAsset asset)
        {
            _playerMap = asset.FindActionMap("Player", throwIfNotFound: true);
            _move = _playerMap.FindAction("Move", throwIfNotFound: true);
            _look = _playerMap.FindAction("Look", throwIfNotFound: true);
            _jump = _playerMap.FindAction("Jump", throwIfNotFound: true);
            _attack = _playerMap.FindAction("Attack", throwIfNotFound: true);
            _attackLeft = _playerMap.FindAction("AttackLeft", throwIfNotFound: true);

            _move.performed += OnMove;
            _move.canceled += OnMove;
            _jump.performed += OnJump;
            _attack.performed += OnAttack;
            _attack.canceled += OnAttackUp;
            _attackLeft.performed += OnAttackLeft;
            _attackLeft.canceled += OnAttackLeftUp;

            _playerMap.Enable();
        }

        private void OnMove(InputAction.CallbackContext ctx) => MoveInput?.Invoke(ctx.ReadValue<Vector2>());
        private void OnJump(InputAction.CallbackContext ctx) => JumpPressed?.Invoke();
        private void OnAttack(InputAction.CallbackContext ctx) => AttackPressed?.Invoke();
        private void OnAttackUp(InputAction.CallbackContext ctx) => AttackReleased?.Invoke();
        private void OnAttackLeft(InputAction.CallbackContext ctx) => AttackLeftPressed?.Invoke();
        private void OnAttackLeftUp(InputAction.CallbackContext ctx) => AttackLeftReleased?.Invoke();

        public void Dispose()
        {
            _move.performed -= OnMove;
            _move.canceled -= OnMove;
            _jump.performed -= OnJump;
            _attack.performed -= OnAttack;
            _attack.canceled -= OnAttackUp;
            _attackLeft.performed -= OnAttackLeft;
            _attackLeft.canceled -= OnAttackLeftUp;
            _playerMap.Disable();
        }
    }
}
