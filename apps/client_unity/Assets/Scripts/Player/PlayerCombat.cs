using UnityEngine;
using VContainer;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;

namespace ProjectAscension.Player
{
    /// <summary>
    /// Drives weapon attacks from input. Right-hand weapon on Attack (LMB),
    /// left-hand on AttackLeft (RMB). Aim comes from the camera pivot. Announces an
    /// execution fact (melee/ranged) when a weapon actually fires; the discovery
    /// relay derives combos (e.g. dodge-then-attack) — combat doesn't know about it.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private Loadout loadout;
        [SerializeField] private Transform aimSource;

        private PlayerInputHandler _input;

        [Inject]
        public void Construct(PlayerInputHandler input) => _input = input;

        private void Start()
        {
            if (_input == null)
            {
                Debug.LogError("[PlayerCombat] PlayerInputHandler not injected.", this);
                enabled = false;
                return;
            }

            _input.AttackPressed += OnLeftClick;
            _input.AttackReleased += OnLeftRelease;
            _input.AttackLeftPressed += OnRightClick;
            _input.AttackLeftReleased += OnRightRelease;

            // A shield defends only while its hand is HELD down. Tell the player's HitReceiver how to
            // ask; BlockRules decides what a raised shield is worth against a given blow.
            if (TryGetComponent<HitReceiver>(out var hitReceiver))
                hitReceiver.Blocking = () => (loadout?.LeftSlot?.Current as ShieldWeapon)?.IsBlocking ?? false;
        }

        private void OnDestroy()
        {
            if (_input == null) return;
            _input.AttackPressed -= OnLeftClick;
            _input.AttackReleased -= OnLeftRelease;
            _input.AttackLeftPressed -= OnRightClick;
            _input.AttackLeftReleased -= OnRightRelease;
        }

        // LMB = right-hand weapon, RMB = left-hand. Press starts (instant weapons fire,
        // charge weapons draw); release fires a charged weapon. Raise the raw click on
        // press for command combos.
        private void OnLeftClick()
        {
            GameplayEvents.RaiseLeftClicked();
            FireDown(loadout != null ? loadout.RightSlot : null);
        }

        private void OnLeftRelease() => FireUp(loadout != null ? loadout.RightSlot : null);

        private void OnRightClick()
        {
            GameplayEvents.RaiseRightClicked();
            FireDown(loadout != null ? loadout.LeftSlot : null);
        }

        private void OnRightRelease() => FireUp(loadout != null ? loadout.LeftSlot : null);

        private void FireDown(EquipmentSlot slot)
        {
            if (!TryWeapon(slot, out var weapon, out var ctx) || !weapon.PrimaryDown(ctx)) return;
            Announce(weapon);
        }

        // WHAT was used, not just that something was: fusing the two hands is an act to be observed,
        // not a thing to be guessed at from the loadout (ADR 0008).
        private void Announce(WeaponBase weapon)
        {
            // The coarse fact (for the tutorial/contracts) and the precise one (what it was made WITH).
            // Airborne/charged/blocking are QUALITIES of the act, gathered by ActRecorder — not five
            // separate events (ADR 0009).
            GameplayEvents.RaiseAttacked(weapon.Data.IsMelee);
            GameplayEvents.RaiseWeaponUsed(EquipmentTags.For(weapon.Data));
        }

        private void FireUp(EquipmentSlot slot)
        {
            if (!TryWeapon(slot, out var weapon, out var ctx) || !weapon.PrimaryUp(ctx)) return;
            Announce(weapon);
        }

        private bool TryWeapon(EquipmentSlot slot, out WeaponBase weapon, out AttackContext ctx)
        {
            weapon = slot?.Current as WeaponBase;
            ctx = default;
            if (weapon == null || aimSource == null) return false;
            ctx = new AttackContext(aimSource.position, aimSource.forward, gameObject);
            return true;
        }
    }
}
