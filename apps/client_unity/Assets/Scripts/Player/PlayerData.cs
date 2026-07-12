using UnityEngine;
using ProjectAscension.GameSimulation.Player;

namespace ProjectAscension.Player
{
    /// <summary>
    /// Tunable player numbers. Authored as an asset so movement/camera values are
    /// never hardcoded. Movement values are fed straight into the shared
    /// <see cref="PlayerSimulation"/> so client prediction matches server authority.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Ascension/Player Data", fileName = "PlayerData")]
    public sealed class PlayerData : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpVelocity = 6f;
        [SerializeField] private float gravity = 20f;
        [SerializeField] private float groundY = 0f;

        [Header("Dodge")]
        [SerializeField] private float dodgeSpeed = 12f;
        [SerializeField] private float dodgeDuration = 0.2f;
        [Range(0f, 1f)]
        [SerializeField] private float dodgeIFrameFraction = 0.75f; // leading fraction of the dodge that is invulnerable

        [Header("Wall-climb (discovered)")]
        [SerializeField] private float wallClimbSpeed = 4f;

        [Header("Camera")]
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        public float LookSensitivity => lookSensitivity;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;

        /// <summary>Builds the authoritative movement settings consumed by the simulation.
        /// Uses the DB-driven player stats when fetched (so balance edits apply with no
        /// rebuild), falling back to the authored values offline. groundY is level geometry,
        /// always authored.</summary>
        public MovementSettings ToMovementSettings()
        {
            var s = PlayerStatsCatalog.Current;
            // Movement capabilities come from discovered skills' effect graphs (ADR 0007).
            int extraJumps = MovementCapabilityCatalog.ExtraJumps; // double jump
            bool wallClimb = MovementCapabilityCatalog.WallClimb;
            return s == null
                ? new(moveSpeed, jumpVelocity, gravity, groundY, dodgeSpeed, dodgeDuration, extraJumps, wallClimb, wallClimbSpeed, dodgeIFrameFraction)
                : new(s.MoveSpeed, s.JumpVelocity, s.Gravity, groundY, s.DodgeSpeed, s.DodgeDuration, extraJumps, wallClimb, wallClimbSpeed, dodgeIFrameFraction);
        }
    }
}
