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
        private readonly InputAction _interact;
        private readonly InputAction _reload;

        public event Action<Vector2> MoveInput;
        public event Action JumpPressed;

        /// <summary>Primary attack (right-hand weapon) — pressed / released (for charge).</summary>
        public event Action AttackPressed;
        public event Action AttackReleased;

        /// <summary>Secondary attack (left-hand weapon) — pressed / released (for charge).</summary>
        public event Action AttackLeftPressed;
        public event Action AttackLeftReleased;

        /// <summary>Press-to-interact — the sensor on the player fires whatever is currently its
        /// target (InteractionSensor.Current), nothing here decides what that is.</summary>
        public event Action InteractPressed;

        /// <summary>Press-to-reload — reloads every equipped weapon with a magazine that isn't
        /// already full (both hands, so dual pistols both reload).</summary>
        public event Action ReloadPressed;

        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 Look => _look.ReadValue<Vector2>();

        /// <summary>Human-readable label for the Interact binding (e.g. "F"), read from the action's
        /// actual binding rather than hardcoded so a rebind updates every prompt with it. Static
        /// because the prompt HUD is plain IMGUI with no DI access to a handler instance, and the
        /// slice only ever has one active binding scheme.</summary>
        public static string InteractKeyLabel { get; private set; } = "F";

        /// <summary>Human-readable label for the Reload binding (e.g. "R") — same rationale as
        /// <see cref="InteractKeyLabel"/>, so the HUD's empty-magazine hint tracks a rebind.</summary>
        public static string ReloadKeyLabel { get; private set; } = "R";

        public PlayerInputHandler(InputActionAsset asset)
        {
            _playerMap = asset.FindActionMap("Player", throwIfNotFound: true);
            _move = _playerMap.FindAction("Move", throwIfNotFound: true);
            _look = _playerMap.FindAction("Look", throwIfNotFound: true);
            _jump = _playerMap.FindAction("Jump", throwIfNotFound: true);
            _attack = _playerMap.FindAction("Attack", throwIfNotFound: true);
            _attackLeft = _playerMap.FindAction("AttackLeft", throwIfNotFound: true);
            _interact = _playerMap.FindAction("Interact", throwIfNotFound: true);
            _reload = _playerMap.FindAction("Reload", throwIfNotFound: true);

            _move.performed += OnMove;
            _move.canceled += OnMove;
            _jump.performed += OnJump;
            _attack.performed += OnAttack;
            _attack.canceled += OnAttackUp;
            _attackLeft.performed += OnAttackLeft;
            _attackLeft.canceled += OnAttackLeftUp;
            _interact.performed += OnInteract;
            _reload.performed += OnReload;

            if (_interact.bindings.Count > 0)
                InteractKeyLabel = _interact.GetBindingDisplayString(0);
            if (_reload.bindings.Count > 0)
                ReloadKeyLabel = _reload.GetBindingDisplayString(0);

            _playerMap.Enable();
        }

        private void OnMove(InputAction.CallbackContext ctx) => MoveInput?.Invoke(ctx.ReadValue<Vector2>());
        private void OnJump(InputAction.CallbackContext ctx) => JumpPressed?.Invoke();
        private void OnAttack(InputAction.CallbackContext ctx) => AttackPressed?.Invoke();
        private void OnAttackUp(InputAction.CallbackContext ctx) => AttackReleased?.Invoke();
        private void OnAttackLeft(InputAction.CallbackContext ctx) => AttackLeftPressed?.Invoke();
        private void OnAttackLeftUp(InputAction.CallbackContext ctx) => AttackLeftReleased?.Invoke();
        private void OnInteract(InputAction.CallbackContext ctx) => InteractPressed?.Invoke();
        private void OnReload(InputAction.CallbackContext ctx) => ReloadPressed?.Invoke();

        public void Dispose()
        {
            _move.performed -= OnMove;
            _move.canceled -= OnMove;
            _jump.performed -= OnJump;
            _attack.performed -= OnAttack;
            _attack.canceled -= OnAttackUp;
            _attackLeft.performed -= OnAttackLeft;
            _attackLeft.canceled -= OnAttackLeftUp;
            _interact.performed -= OnInteract;
            _reload.performed -= OnReload;
            _playerMap.Disable();
        }
    }
}
