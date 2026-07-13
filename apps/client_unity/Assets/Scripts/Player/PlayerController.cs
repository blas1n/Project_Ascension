using UnityEngine;
using VContainer;
using ProjectAscension.Combat;

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
        private HitReceiver _hitReceiver;
        private Vector3 _spawnPoint;

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

            _spawnPoint = transform.position;
            _hitReceiver = GetComponent<HitReceiver>();
            if (_hitReceiver != null)
            {
                // DB-driven max health when fetched (balance edits apply with no rebuild),
                // else the HitReceiver's authored value.
                var stats = GameSimulation.Player.PlayerStatsCatalog.Current;
                if (stats != null) _hitReceiver.SetMaxHealth(stats.MaxHealth);
                _hitReceiver.Died += OnDied;
            }

            // NOT "lock the cursor" — "put the cursor where focus says it goes". The character-creation
            // form is already open when this scene loads, and locking here took the cursor away from
            // its name field: you could not click it, so you could not type your own name.
            UiFocus.ApplyCursor();
        }

        private void OnDied(HitReceiver hitReceiver)
        {
            Debug.Log("[PlayerController] Player died — respawning.");
            GameplayEvents.RaisePlayerDied(); // the delegation tutorial's teachable moment
            _movement.Teleport(_spawnPoint);
            hitReceiver.Revive();
        }

        private void OnDestroy()
        {
            if (_hitReceiver != null)
                _hitReceiver.Died -= OnDied;
            if (_input == null) return;
            _input.JumpPressed -= _movement.QueueJump;
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
