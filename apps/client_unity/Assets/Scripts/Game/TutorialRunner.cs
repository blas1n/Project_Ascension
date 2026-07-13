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

        private const float TravelToCountAsMoved = 4f; // metres before "you have moved" reads as true

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
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            GameplayEvents.Jumped -= OnJumped;
            GameplayEvents.AttackEvaded -= OnEvaded;
            GameplayEvents.Attacked -= OnAttacked;
            GameplayEvents.PlayerDied -= OnPlayerDied;
            GameplayEvents.SkillDiscovered -= OnSkillDiscovered;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            UnbindContracts();
            if (Instance == this) Instance = null;
        }

        /// <summary>Report a fact to the director. Public so systems that land later (the map item,
        /// character creation) can report their beat without this class knowing about them.</summary>
        public void Signal(TutorialSignal signal) => Progress = TutorialDirector.Observe(Progress, signal);

        private void OnJumped() => Signal(TutorialSignal.Jumped);
        private void OnEvaded() => Signal(TutorialSignal.Evaded);
        private void OnAttacked(bool _) => Signal(TutorialSignal.Attacked);
        private void OnPlayerDied() => Signal(TutorialSignal.Died);
        private void OnSkillDiscovered(string _) => Signal(TutorialSignal.DiscoveryMade);

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
            if (_travelled >= TravelToCountAsMoved) Signal(TutorialSignal.Moved);
        }

        private void OnGUI()
        {
            var prompt = PromptFor(Progress);
            if (string.IsNullOrEmpty(prompt)) return;

            const float w = 560f, h = 30f;
            var rect = new Rect((Screen.width - w) * 0.5f, 28f, w, h);

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
            };
            style.normal.textColor = Color.white;

            GUI.Label(rect, prompt, style);
        }

        /// <summary>Minimal, present-tense prompts — what to DO, never an explanation.</summary>
        private static string PromptFor(TutorialProgress progress) => progress.Step switch
        {
            TutorialStep.CreateCharacter => "",
            TutorialStep.Training => TrainingPrompt(progress),
            TutorialStep.ChooseEquipment => "Choose two pieces of equipment.",
            TutorialStep.FirstDiscovery => "Fight your own way — discovery comes from how you act.",
            TutorialStep.AcceptSurveyContract => "Press [F] at the board to take a contract.",
            TutorialStep.EarnMap => "Survey the outskirts. Reach the marker.",
            TutorialStep.AcceptDeepContract => "Press [F] at the board to take the next contract.",
            TutorialStep.FirstDeath => "Go deeper.",
            TutorialStep.DelegateContract => "You cannot finish this alone. Delegate it (위임).",
            TutorialStep.IssueContract => "Then hire someone who can. Issue a contract (발주).",
            TutorialStep.Return => "Press [F] at the return pad to go back to the city.",
            _ => "",
        };

        private static string TrainingPrompt(TutorialProgress progress)
        {
            var left = TutorialDirector.RemainingTraining(progress);
            if (left == TutorialSignal.None) return "";

            // Only ask for what's still outstanding — never tell the player to do what they just did.
            var parts = new System.Collections.Generic.List<string>(4);
            if ((left & TutorialSignal.Moved) != 0) parts.Add("Move");
            if ((left & TutorialSignal.Jumped) != 0) parts.Add("Jump");
            if ((left & TutorialSignal.Evaded) != 0) parts.Add("Step out of range during a monster's wind-up");
            if ((left & TutorialSignal.Attacked) != 0) parts.Add("Attack");
            return string.Join("   ·   ", parts);
        }
    }
}
