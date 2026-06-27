using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Fires discovered Commands by their assigned button combo (the rule engine gives
    /// each command an "incantation" — e.g. Jump → RightClick → LeftClick — decoupled
    /// from the behaviors that discovered it, so double jump and dodge-slash invoke the
    /// same way). Subscribes the raw button facts on <see cref="GameplayEvents"/>, feeds
    /// a deterministic <see cref="ComboRecognizer"/>, and on a match casts the command
    /// through <see cref="SkillCaster"/> — gated by the equipment binding (ADR 0005).
    /// Weapons are unaffected (they fire on the attack input).
    /// </summary>
    public sealed class ComboInvoker : MonoBehaviour
    {
        [SerializeField] private float comboWindow = ComboRecognizer.DefaultWindow;

        private ComboRecognizer _recognizer;
        private SkillCaster _caster;

        private void Awake()
        {
            _recognizer = new ComboRecognizer(comboWindow);
            _caster = GetComponent<SkillCaster>();
            if (_caster == null) _caster = FindAnyObjectByType<SkillCaster>();
        }

        private void OnEnable()
        {
            GameplayEvents.Jumped += OnJumped;
            GameplayEvents.Dodged += OnDodged;
            GameplayEvents.LeftClicked += OnLeftClicked;
            GameplayEvents.RightClicked += OnRightClicked;
        }

        private void OnDisable()
        {
            GameplayEvents.Jumped -= OnJumped;
            GameplayEvents.Dodged -= OnDodged;
            GameplayEvents.LeftClicked -= OnLeftClicked;
            GameplayEvents.RightClicked -= OnRightClicked;
        }

        /// <summary>Register a discovered command's assigned combo (called when its skill loads).</summary>
        public void RegisterCommand(IReadOnlyList<InputToken> combo, DiscoveredSkill skill)
            => _recognizer.Register(combo, skill);

        private void OnJumped() => Feed(InputToken.Jump);
        private void OnDodged() => Feed(InputToken.Dodge);
        private void OnLeftClicked() => Feed(InputToken.LeftClick);
        private void OnRightClicked() => Feed(InputToken.RightClick);

        private void Feed(InputToken token)
        {
            var command = _recognizer.Feed(token, Time.time);
            if (command == null) return;

            _caster?.ExecuteSkill(command.Skill);
            Debug.Log($"[ComboInvoker] Invoked \"{command.Name}\" via combo.");
        }
    }
}
