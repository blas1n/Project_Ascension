using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Discovery;

namespace ProjectAscension.Player
{
    /// <summary>
    /// The one place that watches the player and says what they just DID (ADR 0009).
    ///
    /// Before this, five different observers each reached for a different fact — one event for an air
    /// attack, another for a charged one, a bespoke window for dodge-attacks, another for weapon
    /// fusion. Every new idea meant a new observer. So instead: one component, on the player, that
    /// knows the player's STATE (are they off the ground? is the shield up? was that shot drawn?), and
    /// emits a single act stream. The grammar downstream decides what those acts add up to; adding
    /// aiming or parrying later means adding a quality here, and nothing else anywhere.
    ///
    /// It reports facts. It decides nothing.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ActRecorder : MonoBehaviour
    {
        [SerializeField] private Loadout loadout;

        private CharacterController _controller;
        private Vector3 _lastPosition;
        private bool _moving;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (loadout == null) loadout = GetComponent<Loadout>();
            _lastPosition = transform.position;
        }

        private void OnEnable()
        {
            GameplayEvents.Jumped += OnJumped;
            GameplayEvents.Dodged += OnDodged;
            GameplayEvents.WeaponUsed += OnWeaponUsed;
        }

        private void OnDisable()
        {
            GameplayEvents.Jumped -= OnJumped;
            GameplayEvents.Dodged -= OnDodged;
            GameplayEvents.WeaponUsed -= OnWeaponUsed;
        }

        private void Update()
        {
            var p = transform.position;
            _moving = (new Vector2(p.x - _lastPosition.x, p.z - _lastPosition.z)).sqrMagnitude > 0.0004f;
            _lastPosition = p;
        }

        private void OnJumped() => Emit("jump", null);
        private void OnDodged() => Emit("dodge", null);

        // An attack names the weapon it was made with — rolling into a gunshot is not the same act as
        // rolling into a sword, and the grammar keeps that difference.
        private void OnWeaponUsed(string contextTag) => Emit("attack", contextTag, ChargedNow());

        private void Emit(string verb, string instrument, bool charged = false)
        {
            var qualities = ActQuality.None;
            if (_controller != null && !_controller.isGrounded) qualities |= ActQuality.Airborne;
            if (charged) qualities |= ActQuality.Charged;
            if (ShieldUp()) qualities |= ActQuality.Blocking;
            if (_moving) qualities |= ActQuality.Moving;

            GameplayEvents.RaiseActPerformed(new Act(verb, instrument, Time.time, qualities));
        }

        /// <summary>Was the shot that just left actually DRAWN, rather than tapped? The threshold is
        /// DB-driven, so what counts as a real charge is a balance decision, not a constant here.</summary>
        private bool ChargedNow()
        {
            var right = loadout?.RightSlot?.Current as WeaponBase;
            var left = loadout?.LeftSlot?.Current as WeaponBase;
            float charge = Mathf.Max(right?.LastCharge ?? 0f, left?.LastCharge ?? 0f);
            return charge >= GameSimulation.Combat.CombatTuningCatalog.Current.ChargedAttackThreshold;
        }

        private bool ShieldUp() => (loadout?.LeftSlot?.Current as ShieldWeapon)?.IsBlocking ?? false;
    }
}
