using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;
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
        private readonly HashSet<DiscoveredSkill> _registered = new HashSet<DiscoveredSkill>();

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

        // Register every discovered Command from the session's set that we haven't yet — the
        // set is the single source of truth (populated by the session-start restore AND by new
        // in-frontier discoveries), so this is robust to load ORDER: whenever a command exists
        // in the set, its combo becomes recognizable, no matter when it arrived.
        private void SyncFromSet()
        {
            var set = GameSession.Instance != null ? GameSession.Instance.DiscoveredSkills : null;
            if (set == null) return;
            foreach (var command in set.Commands)
                if (_registered.Add(command))
                    _recognizer.Register(command.Combo, command);
        }

        private void OnJumped() => Feed(InputToken.Jump);
        private void OnDodged() => Feed(InputToken.Dodge);
        private void OnLeftClicked() => Feed(InputToken.LeftClick);
        private void OnRightClicked() => Feed(InputToken.RightClick);

        private Loadout _loadout;

        private void Feed(InputToken token)
        {
            SyncFromSet(); // pick up any commands added since the last input
            var command = _recognizer.Feed(token, Time.time);
            if (command != null) TryInvoke(command);
        }

        private void TryInvoke(DiscoveredSkill command)
        {
            // ADR 0005 (재개정): a command whose combo uses a weapon click can only be invoked
            // with the weapon category it was discovered with (a flame+gun technique isn't
            // reproducible with a sword). Behaviour-only combos are unrestricted.
            if (!CommandGate.Invocable(command, CurrentEquipment()))
            {
                Debug.Log($"[ComboInvoker] \"{command.Name}\" needs {string.Join("/", CommandGate.RequiredEquipment(command))} equipped.");
                return;
            }

            _caster?.ExecuteSkill(command.Skill);
            Debug.Log($"[ComboInvoker] Invoked \"{command.Name}\" via combo.");
        }

        private HashSet<string> CurrentEquipment()
        {
            if (_loadout == null) _loadout = FindAnyObjectByType<Loadout>();
            return EquipmentTags.CurrentTags(_loadout);
        }
    }
}
