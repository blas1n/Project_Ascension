using UnityEngine;
using VContainer;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;

namespace ProjectAscension.Player
{
    /// <summary>
    /// Drives weapon attacks from input. Right-hand weapon on Attack (LMB),
    /// left-hand on AttackLeft (RMB). Aim comes from the camera pivot.
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

            _input.AttackPressed += FireRight;
            _input.AttackLeftPressed += FireLeft;
        }

        private void OnDestroy()
        {
            if (_input == null) return;
            _input.AttackPressed -= FireRight;
            _input.AttackLeftPressed -= FireLeft;
        }

        private void FireRight() => Fire(loadout != null ? loadout.RightSlot : null);
        private void FireLeft() => Fire(loadout != null ? loadout.LeftSlot : null);

        private void Fire(EquipmentSlot slot)
        {
            if (slot?.Current is not WeaponBase weapon || aimSource == null) return;
            var ctx = new AttackContext(aimSource.position, aimSource.forward, gameObject);
            weapon.PrimaryAction(ctx);
        }
    }
}
