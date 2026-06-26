using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.GameSimulation.Discovery;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Fires discovered Commands by their invocation combo (discovery.md — a command is
    /// the behavior pattern that made it, re-performed; not a dedicated button). Watches
    /// the same gameplay facts as <see cref="BehaviorTracker"/>, feeds them to a
    /// deterministic <see cref="ComboRecognizer"/>, and on a match casts the command
    /// through <see cref="SkillCaster"/> — gated by the skill's equipment binding
    /// (ADR 0005). Weapons are unaffected (those fire on the attack input).
    /// </summary>
    public sealed class ComboInvoker : MonoBehaviour
    {
        [SerializeField] private float comboWindow = ComboRecognizer.DefaultWindow;

        private ComboRecognizer _recognizer;
        private Loadout _loadout;
        private SkillCaster _caster;

        private void Awake()
        {
            _recognizer = new ComboRecognizer(comboWindow);
            _loadout = FindAnyObjectByType<Loadout>();
            _caster = GetComponent<SkillCaster>();
            if (_caster == null) _caster = FindAnyObjectByType<SkillCaster>();
        }

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

        /// <summary>Register a discovered command's combo (called when its skill loads).</summary>
        public void RegisterCommand(IReadOnlyList<BehaviorKind> combo, DiscoveredSkill skill)
            => _recognizer.Register(combo, skill);

        private void OnJumped() => Feed(BehaviorKind.Jump);
        private void OnDodged() => Feed(BehaviorKind.Dodge);
        private void OnAttacked(bool isMelee) => Feed(isMelee ? BehaviorKind.MeleeAttack : BehaviorKind.RangedAttack);

        private void Feed(BehaviorKind kind)
        {
            var command = _recognizer.Feed(kind, Time.time);
            if (command == null) return;

            if (!DiscoveredSkillSet.Usable(command, EquipmentTags.CurrentTags(_loadout)))
            {
                Debug.Log($"[ComboInvoker] \"{command.Name}\" combo matched but the bound equipment is not held.");
                return;
            }

            _caster?.ExecuteSkill(command.Skill);
            Debug.Log($"[ComboInvoker] Invoked \"{command.Name}\" via combo.");
        }
    }
}
