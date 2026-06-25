using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Discovery;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The single discovery observation relay. Subscribes to domain execution facts
    /// (<see cref="GameplayEvents"/>), assembles situational context, derives combos
    /// (dodge-then-attack), and feeds the discovery system. This is the *only* place
    /// that knows about discovery — gameplay systems just announce what happened.
    /// New observation = a new <see cref="GameplayEvents"/> fact + a mapping here + a
    /// catalog entry. (Component name kept as BehaviorTracker for scene stability.)
    /// </summary>
    public sealed class BehaviorTracker : MonoBehaviour
    {
        private const float DodgeAttackWindow = 0.6f;

        private Loadout _loadout;
        private readonly HashSet<string> _context = new();
        private float _lastDodgeTime = -999f;

        private void Start() => _loadout = FindAnyObjectByType<Loadout>();

        private void OnEnable()
        {
            GameplayEvents.Jumped += OnJumped;
            GameplayEvents.Dodged += OnDodged;
            GameplayEvents.Attacked += OnAttacked;
        }

        private void OnDisable()
        {
            GameplayEvents.Jumped -= OnJumped;
            GameplayEvents.Dodged -= OnDodged;
            GameplayEvents.Attacked -= OnAttacked;
        }

        private void OnJumped() => Emit(BehaviorKind.Jump);

        private void OnDodged()
        {
            _lastDodgeTime = Time.time;
            Emit(BehaviorKind.Dodge);
        }

        private void OnAttacked(bool isMelee)
        {
            Emit(isMelee ? BehaviorKind.MeleeAttack : BehaviorKind.RangedAttack);
            // Combo: an attack within the window after an actual dodge.
            if (Time.time - _lastDodgeTime <= DodgeAttackWindow)
                Emit(BehaviorKind.DodgeAttack);
        }

        private void Emit(BehaviorKind kind)
        {
            var session = GameSession.Instance;
            if (session == null) return;

            BuildContext();
            session.Discovery.Observe(new Observation(kind, _context));
        }

        private void BuildContext()
        {
            // Equipment tags now; environment/target tags plug in here later without
            // touching the domain systems or the engine signature.
            _context.Clear();
            if (_loadout == null) return;
            AddTags(_loadout.LeftSlot?.Current?.Data);
            AddTags(_loadout.RightSlot?.Current?.Data);
        }

        private void AddTags(WeaponData data)
        {
            if (data == null) return;
            switch (data.EquipmentType)
            {
                case EquipmentType.Weapon: _context.Add("melee"); break;
                case EquipmentType.Firearm: _context.Add("firearm"); break;
                case EquipmentType.Bow: _context.Add("bow"); break;
                case EquipmentType.Catalyst: _context.Add("arcane"); break;
            }
        }
    }
}
