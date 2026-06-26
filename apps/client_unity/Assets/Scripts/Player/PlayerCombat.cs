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
            _input.AttackLeftPressed += OnRightClick;
        }

        private void OnDestroy()
        {
            if (_input == null) return;
            _input.AttackPressed -= OnLeftClick;
            _input.AttackLeftPressed -= OnRightClick;
        }

        // Raise the raw click (for command combos) then fire the weapon.
        private void OnLeftClick()
        {
            GameplayEvents.RaiseLeftClicked();
            Fire(loadout != null ? loadout.RightSlot : null);
        }

        private void OnRightClick()
        {
            GameplayEvents.RaiseRightClicked();
            Fire(loadout != null ? loadout.LeftSlot : null);
        }

        private void Fire(EquipmentSlot slot)
        {
            if (slot?.Current is not WeaponBase weapon || aimSource == null) return;

            var ctx = new AttackContext(aimSource.position, aimSource.forward, gameObject);
            if (!weapon.PrimaryAction(ctx)) return; // on cooldown

            GameplayEvents.RaiseAttacked(weapon.Data.IsMelee);
        }
    }
}
