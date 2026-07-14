using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Tutorial;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The first hour's shell adapter (docs/03-gameplay/first-hour-experience.md): it feeds real
    /// gameplay FACTS to the pure <see cref="TutorialDirector"/> (which owns every decision — ADR:
    /// Unity is a shell) and renders the current step's prompt. No step is completed by a "next"
    /// button; the player advances by doing the thing. Prompts stay minimal — the doc's rule is that
    /// the player learns by experience, not explanation.
    /// </summary>
    public sealed class TutorialRunner : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<TutorialRunner>() != null) return;
            var go = new GameObject("TutorialRunner");
            DontDestroyOnLoad(go); // the first hour spans city <-> frontier
            go.AddComponent<TutorialRunner>();
        }

        public static TutorialRunner Instance { get; private set; }

        public TutorialProgress Progress { get; private set; } = TutorialProgress.Start;

        /// <summary>Raised when the director's step actually advances (never fires for a repeated or
        /// out-of-order signal — TutorialDirector.Observe already guarantees Step only moves forward).
        /// TutorialGuide is the reason this exists: it needs to know the moment a NEW step begins so
        /// it can walk up and say the new line, without polling Progress every frame itself.</summary>
        public event Action<TutorialStep> StepChanged;

        private ContractService _contracts;
        private Transform _player;
        private Vector3 _lastPlayerPos;
        private float _travelled;

        private void Awake()
        {
            Instance = this;

            GameplayEvents.Jumped += OnJumped;
            GameplayEvents.AttackEvaded += OnEvaded;
            GameplayEvents.Attacked += OnAttacked;
            GameplayEvents.PlayerDied += OnPlayerDied;
            GameplayEvents.SkillDiscovered += OnSkillDiscovered;
            GameplayEvents.EquipmentChosen += OnEquipmentChosen;
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            GameplayEvents.Jumped -= OnJumped;
            GameplayEvents.AttackEvaded -= OnEvaded;
            GameplayEvents.Attacked -= OnAttacked;
            GameplayEvents.PlayerDied -= OnPlayerDied;
            GameplayEvents.SkillDiscovered -= OnSkillDiscovered;
            GameplayEvents.EquipmentChosen -= OnEquipmentChosen;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            UnbindContracts();
            if (Instance == this) Instance = null;
        }

        /// <summary>Report a fact to the director. Public so systems that land later (the map item,
        /// character creation) can report their beat without this class knowing about them.</summary>
        public void Signal(TutorialSignal signal)
        {
            var before = Progress.Step;
            Progress = TutorialDirector.Observe(Progress, signal);
            if (Progress.Step != before) StepChanged?.Invoke(Progress.Step);
        }

        private void OnJumped() => Signal(TutorialSignal.Jumped);
        private void OnEvaded() => Signal(TutorialSignal.Evaded);
        private void OnAttacked(bool _) => Signal(TutorialSignal.Attacked);
        private void OnPlayerDied() => Signal(TutorialSignal.Died);
        private void OnSkillDiscovered(string _, GameSimulation.Combat.ManifestationKind __) => Signal(TutorialSignal.DiscoveryMade);
        private void OnEquipmentChosen() => Signal(TutorialSignal.EquipmentChosen);

        private void OnSceneChanged(Scene _, Scene next)
        {
            if (next.name == GameScenes.City) Signal(TutorialSignal.ReturnedToCity);
        }

        private void Update()
        {
            BindContracts();
            TrackMovement();
        }

        // The contract service is owned by GameSession, which spins up after this runner installs.
        private void BindContracts()
        {
            var session = GameSession.Instance;
            if (session == null || session.Contracts == null || ReferenceEquals(_contracts, session.Contracts)) return;

            UnbindContracts();
            _contracts = session.Contracts;
            _contracts.Accepted += OnContractAccepted;
            _contracts.HandedOff += OnContractHandedOff;
            _contracts.Issued += OnContractIssued;
        }

        private void UnbindContracts()
        {
            if (_contracts == null) return;
            _contracts.Accepted -= OnContractAccepted;
            _contracts.HandedOff -= OnContractHandedOff;
            _contracts.Issued -= OnContractIssued;
            _contracts = null;
        }

        // Which contract beat this is depends on where the player is in the sequence, not on the
        // contract's content — so authored copy can change without breaking the first hour.
        private void OnContractAccepted(GameSimulation.Contracts.ContractInstance _)
        {
            if (Progress.Step == TutorialStep.AcceptSurveyContract) Signal(TutorialSignal.SurveyContractAccepted);
            else if (Progress.Step == TutorialStep.AcceptDeepContract) Signal(TutorialSignal.DeepContractAccepted);
        }

        private void OnContractHandedOff(GameSimulation.Contracts.ContractInstance _) => Signal(TutorialSignal.ContractDelegated);
        private void OnContractIssued(GameSimulation.Contracts.ContractInstance _) => Signal(TutorialSignal.ContractIssued);

        // "Moved" is real travel, not a twitch of the stick — so the prompt clears when the player has
        // actually gone somewhere.
        private void TrackMovement()
        {
            if ((Progress.Seen & TutorialSignal.Moved) != 0) return;

            if (_player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go == null) return;
                _player = go.transform;
                _lastPlayerPos = _player.position;
                return;
            }

            var p = _player.position;
            _travelled += Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(_lastPlayerPos.x, 0f, _lastPlayerPos.z));
            _lastPlayerPos = p;
            if (TutorialDirector.HasTravelledEnoughToCountAsMoved(_travelled)) Signal(TutorialSignal.Moved);
        }

        // The persistent objective tracker (playtest, 2nd report: "all I see is dialogue at the top
        // of the screen" — this WAS that floating string; it is now a proper framed panel, always
        // visible, top-center — clear of ContractHud (top-left), SkillGuideHud (top-right), the
        // ability bar / health bar (bottom-center) and the magazine (bottom-right)). The headline is
        // never authored twice: it reads TutorialGuideScript.For(step).Objective, the SAME pure model
        // the guide's own dialogue reads — this class adds only the training step's live checklist,
        // which is real gameplay state (Progress.Seen), not a second copy of the script.
        private void OnGUI()
        {
            var objective = TutorialGuideScript.For(Progress.Step).Objective;
            if (string.IsNullOrEmpty(objective)) return;

            var checklist = Progress.Step == TutorialStep.Training ? TrainingChecklist(Progress) : "";

            const float w = 460f;
            float h = string.IsNullOrEmpty(checklist) ? 54f : 76f;
            var box = new Rect((Screen.width - w) * 0.5f, 16f, w, h);

            GUI.Box(box, "Objective");

            var body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, fontStyle = FontStyle.Bold };
            body.normal.textColor = Color.white;
            GUI.Label(new Rect(box.x + 14f, box.y + 22f, box.width - 28f, 30f), objective, body);

            if (!string.IsNullOrEmpty(checklist))
            {
                var sub = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
                sub.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);
                GUI.Label(new Rect(box.x + 14f, box.y + 52f, box.width - 28f, 22f), checklist, sub);
            }
        }

        /// <summary>Training's live sub-checklist — real gameplay state (what's still outstanding),
        /// never a re-authored copy of the objective line above it; only asks for what the player
        /// hasn't already done.</summary>
        private static string TrainingChecklist(TutorialProgress progress)
        {
            var left = TutorialDirector.RemainingTraining(progress);
            if (left == TutorialSignal.None) return "";

            var parts = new System.Collections.Generic.List<string>(4);
            if ((left & TutorialSignal.Moved) != 0) parts.Add("Move");
            if ((left & TutorialSignal.Jumped) != 0) parts.Add("Jump");
            if ((left & TutorialSignal.Evaded) != 0) parts.Add("Step out of range during a monster's wind-up");
            if ((left & TutorialSignal.Attacked) != 0) parts.Add("Attack");
            return "Still to do: " + string.Join("   ·   ", parts);
        }
    }
}
