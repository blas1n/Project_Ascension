using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAscension.Combat;
using ProjectAscension.GameSimulation.Tutorial;
using ProjectAscension.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The dedicated first-hour guide — a PER-PLAYER INSTANCED NPC, not a shared world character. The
    /// playtest report was right that the authored first hour (docs/03-gameplay/first-hour-experience.md)
    /// existed only in the simulation — the player was just dropped in the city with nothing telling
    /// them what to do. The fix is NOT a single NPC that walks over to greet every player: that cannot
    /// work once more than one player exists (this is an MMOFPS), so it is not "the" guide, it is A
    /// guide — spawned fresh for whoever's first hour this is, built into every scene the first hour
    /// touches by ProjectAscensionSetup (City, Frontier), and gone for good once
    /// <see cref="TutorialStep.Complete"/> is reached. Nothing here reads or writes any shared state:
    /// two players standing in the same plaza each get their own Usher, at their own pace, saying
    /// whatever THEIR <see cref="TutorialRunner"/> currently says.
    ///
    /// It READS the pure <see cref="TutorialGuideScript"/> for what to say and where to point; it makes
    /// no decision about progression (TutorialDirector's job, and only its job — this class never calls
    /// TutorialRunner.Signal). On a new step it walks up to the player and opens a dialogue popup
    /// (which takes <see cref="UiFocus"/>, same discipline as every other modal in the city, so the
    /// player can't wander off mid-line by accident); once dismissed it steps back out of the way
    /// rather than gluing itself to the player's heel. It faces whatever station the line points at
    /// while it talks — the closest thing to actually pointing a finger.
    ///
    /// Procedural body (no authored art yet, same seam as MonsterBody/CityNpc) — its own hooded,
    /// lantern-carrying silhouette and warm gold palette, distinct from every monster and every other
    /// city NPC, so it reads as "not part of the world, here for YOU" at a glance.
    /// </summary>
    public sealed class TutorialGuide : MonoBehaviour
    {
        private const string DisplayName = "Usher";

        private static readonly Color Robe = new Color(0.82f, 0.64f, 0.22f);
        private static readonly Color RobeDark = new Color(0.82f * 0.6f, 0.64f * 0.6f, 0.22f * 0.6f);
        private static readonly Color Trim = Color.Lerp(Robe, Color.white, 0.55f);
        private static readonly Color LanternGlow = new Color(1f, 0.85f, 0.45f);

        private const float MoveSpeed = 6.5f;
        private const float TurnDegreesPerSecond = 260f;
        private const float ArriveEpsilon = 0.15f;
        private const float LeaveDistance = 9f;

        private enum GuideState { Idle, Approaching, Speaking, Retreating, Leaving, Gone }

        private GuideState _state = GuideState.Idle;
        private Transform _player;
        private TutorialRunner _runner;
        private string _line = "";
        private TutorialGuideStation _station = TutorialGuideStation.None;
        private bool _dialogueOpen;
        private Vector3 _leaveTarget;

        private void Awake() => Build();

        private void OnDestroy()
        {
            if (_runner != null) _runner.StepChanged -= OnStepChanged;
            if (_dialogueOpen) { UiFocus.Pop(); _dialogueOpen = false; } // must Pop even if the scene changes mid-dialogue
        }

        private void Update()
        {
            if (_player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go == null) return;
                _player = go.transform;
            }

            if (_runner == null)
            {
                var runner = TutorialRunner.Instance;
                if (runner == null) return;
                BindRunner(runner);
            }

            switch (_state)
            {
                case GuideState.Idle:
                    MoveToward(FollowPoint(), null);
                    break;
                case GuideState.Approaching:
                    MoveToward(ApproachPoint(), OnArrivedToSpeak);
                    break;
                case GuideState.Speaking:
                    FaceStationOrPlayer();
                    if (DismissPressed()) CloseDialogue();
                    break;
                case GuideState.Retreating:
                    MoveToward(FollowPoint(), OnArrivedRetreat);
                    break;
                case GuideState.Leaving:
                    MoveToward(_leaveTarget, OnArrivedLeave);
                    break;
                case GuideState.Gone:
                    break;
            }
        }

        // --- director plumbing -----------------------------------------------------------------

        private void BindRunner(TutorialRunner runner)
        {
            _runner = runner;
            _runner.StepChanged += OnStepChanged;

            var step = runner.Progress.Step;
            if (step == TutorialStep.Complete)
            {
                // A guide freshly built into a scene the player reaches AFTER finishing the first
                // hour this session has nothing left to do — no walk-in, no line, just gone.
                _state = GuideState.Gone;
                gameObject.SetActive(false);
                return;
            }

            // CreateCharacter is a screen, not a place — the character sheet already owns the moment
            // (and the UiFocus gate). The guide waits quietly nearby for the step that follows it.
            if (step != TutorialStep.CreateCharacter) BeginApproachFor(step);
        }

        private void OnStepChanged(TutorialStep step)
        {
            if (step == TutorialStep.Complete) { BeginLeaving(); return; }
            BeginApproachFor(step);
        }

        private void BeginApproachFor(TutorialStep step)
        {
            var line = TutorialGuideScript.For(step);
            _line = line.Text;
            _station = line.Station;
            if (string.IsNullOrEmpty(_line)) return; // defensive — every non-Complete step has a line

            // The newest step always wins, even mid-conversation: the player already lived it, so an
            // abrupt cut to the next line beats a stale one.
            _state = GuideState.Approaching;
        }

        private void OnArrivedToSpeak()
        {
            _state = GuideState.Speaking;
            if (!_dialogueOpen) { UiFocus.Push(); _dialogueOpen = true; }
        }

        private void CloseDialogue()
        {
            if (_dialogueOpen) { UiFocus.Pop(); _dialogueOpen = false; }
            _state = GuideState.Retreating;
        }

        private void OnArrivedRetreat() => _state = GuideState.Idle;

        private void BeginLeaving()
        {
            if (_dialogueOpen) { UiFocus.Pop(); _dialogueOpen = false; }
            var awayDir = _player != null ? Flatten(transform.position - _player.position) : transform.forward;
            _leaveTarget = transform.position + awayDir * LeaveDistance;
            _state = GuideState.Leaving;
        }

        private void OnArrivedLeave()
        {
            _state = GuideState.Gone;
            gameObject.SetActive(false);
        }

        // --- movement ----------------------------------------------------------------------------

        private void MoveToward(Vector3 target, System.Action onArrive)
        {
            var pos = transform.position;
            var flatTarget = new Vector3(target.x, pos.y, target.z);
            var next = Vector3.MoveTowards(pos, flatTarget, MoveSpeed * Time.deltaTime);
            transform.position = next;

            var dir = flatTarget - next;
            if (dir.sqrMagnitude > 0.01f) FaceDirection(dir);

            if ((next - flatTarget).sqrMagnitude <= ArriveEpsilon * ArriveEpsilon)
                onArrive?.Invoke();
        }

        private void FaceDirection(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            var target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnDegreesPerSecond * Time.deltaTime);
        }

        private void FaceStationOrPlayer()
        {
            if (_station != TutorialGuideStation.None && TutorialGuideStations.TryResolve(_station, out var stationPos))
                FaceDirection(stationPos - transform.position);
            else if (_player != null)
                FaceDirection(_player.position - transform.position);
        }

        // Just ahead and to the side of the player — enters view instead of appearing behind them.
        private Vector3 ApproachPoint() =>
            _player.position + Flatten(_player.forward) * 2.2f + Flatten(_player.right) * 0.9f;

        // A polite distance off to the side, never blocking the way forward.
        private Vector3 FollowPoint() =>
            _player.position - Flatten(_player.forward) * 0.5f + Flatten(_player.right) * 3.2f;

        private static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        // --- dialogue ------------------------------------------------------------------------------

        private static bool DismissPressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Keyboard.current == null) return false;
            if (System.Enum.TryParse<Key>(PlayerInputHandler.InteractKeyLabel, true, out var key) &&
                Keyboard.current[key].wasPressedThisFrame) return true;
            return Keyboard.current.fKey.wasPressedThisFrame; // fallback if the label doesn't parse as a Key
        }

        private void OnGUI()
        {
            if (_state != GuideState.Speaking) return;

            // A light veil, not a blackout — you are standing in the open world, not at a station.
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.32f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            const float w = 620f, h = 100f;
            var box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.6f, w, h);
            GUI.Box(box, GUIContent.none);

            var name = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 13, fontStyle = FontStyle.Bold };
            name.normal.textColor = new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(box.x, box.y + 8f, box.width, 18f), DisplayName, name);

            var say = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, wordWrap = true };
            say.normal.textColor = Color.white;
            GUI.Label(new Rect(box.x + 16f, box.y + 26f, box.width - 32f, 44f), _line, say);

            var hint = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 11 };
            hint.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            GUI.Label(new Rect(box.x, box.y + h - 20f, box.width, 18f),
                $"[{PlayerInputHandler.InteractKeyLabel}] or click to continue", hint);
        }

        // --- body ----------------------------------------------------------------------------------

        // A hooded, lantern-carrying silhouette — deliberately unlike CityNpc's plain box-and-head
        // (Quartermaster/Serjeant/Clerk) and unlike any MonsterBody shape, so it reads at a glance as
        // "not part of the world, here for you". No colliders anywhere: it is here to help, never to
        // physically block the player it's following.
        private void Build()
        {
            Part("Robe", new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 1.7f, 0.52f), Robe);
            Part("Shoulders", new Vector3(0f, 1.55f, 0f), new Vector3(0.82f, 0.28f, 0.58f), RobeDark);
            var hood = Part("Hood", new Vector3(0f, 1.85f, 0.06f), new Vector3(0.48f, 0.42f, 0.48f), RobeDark);
            hood.transform.localRotation = Quaternion.Euler(14f, 0f, 0f); // tips forward, face hidden
            Part("Sash", new Vector3(0f, 1.05f, 0.27f), new Vector3(0.48f, 0.16f, 0.05f), Trim);

            Part("StaffArm", new Vector3(0.34f, 1.12f, 0.1f), new Vector3(0.15f, 0.7f, 0.15f), RobeDark);
            Part("Staff", new Vector3(0.52f, 1.5f, 0.14f), new Vector3(0.07f, 1.55f, 0.07f), Trim);
            var lantern = Part("Lantern", new Vector3(0.52f, 2.22f, 0.14f), new Vector3(0.2f, 0.2f, 0.2f), LanternGlow);
            var lanternRenderer = lantern.GetComponent<Renderer>();
            if (lanternRenderer != null) lanternRenderer.material = CombatVfx.Glow(LanternGlow); // the light it carries

            Part("Leg_L", new Vector3(-0.18f, 0.14f, 0f), new Vector3(0.2f, 0.32f, 0.2f), RobeDark);
            Part("Leg_R", new Vector3(0.18f, 0.14f, 0f), new Vector3(0.2f, 0.32f, 0.2f), RobeDark);
        }

        private GameObject Part(string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
            return go;
        }
    }
}
