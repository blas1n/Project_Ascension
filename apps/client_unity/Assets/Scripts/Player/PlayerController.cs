using UnityEngine;
using VContainer;

namespace ProjectAscension.Player
{
    /// <summary>
    /// Scene-side glue for the player. Owns only Unity lifecycle; all behaviour
    /// lives in the injected handlers. Camera updates per frame, movement on the
    /// fixed step for a deterministic simulation timestep.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;

        private PlayerInputHandler _input;
        private PlayerMovement _movement;
        private PlayerCamera _camera;

        [Inject]
        public void Construct(PlayerInputHandler input, PlayerMovement movement, PlayerCamera camera)
        {
            _input = input;
            _movement = movement;
            _camera = camera;
        }

        private void Start()
        {
            if (_movement == null || _camera == null || _input == null)
            {
                Debug.LogError(
                    "[PlayerController] Dependencies were not injected. Ensure a FrontierLifetimeScope " +
                    "with InputActions and PlayerData assigned exists in the scene " +
                    "(re-run Project Ascension > Setup > Build All Scenes). Disabling to avoid error spam.", this);
                enabled = false;
                return;
            }

            var controller = GetComponent<CharacterController>();
            _movement.Initialize(controller, transform);
            _camera.Initialize(transform, cameraPivot);

            _input.JumpPressed += _movement.QueueJump;
            _input.DodgePressed += _movement.QueueDodge;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDestroy()
        {
            if (_input == null) return;
            _input.JumpPressed -= _movement.QueueJump;
            _input.DodgePressed -= _movement.QueueDodge;
        }

        private void Update()
        {
            _camera.Tick(_input.Look);
        }

        private void FixedUpdate()
        {
            _movement.Tick(_input.Move, Time.fixedDeltaTime);
        }
    }
}
