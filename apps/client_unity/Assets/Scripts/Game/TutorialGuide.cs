using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    /// guide — spawned fresh for whoever's first hour this is, self-installed into every scene the
    /// first hour touches (City, Frontier — see <see cref="Install"/> below), and gone for good once
    /// <see cref="TutorialStep.Complete"/> is reached. Nothing here reads or writes any shared state:
    /// two players standing in the same plaza each get their own Usher, at their own pace, saying
    /// whatever THEIR <see cref="TutorialRunner"/> currently says.
    ///
    /// It READS the pure <see cref="TutorialGuideScript"/> for what to say and where to point; it makes
    /// no decision about progression (TutorialDirector's job, and only its job — this class never calls
    /// TutorialRunner.Signal). On a new step it walks up to the player and opens a dialogue popup
    /// (which takes <see cref="UiFocus"/>, same discipline as every other modal in the city, so the
    /// player can't wander off mid-line by accident). It faces whatever station the line points at
    /// while it talks — the closest thing to actually pointing a finger.
    ///
    /// Playtest (a person just vanishing after the dialogue reads as broken, and gives the player
    /// nothing to follow): once dismissed, the guide does NOT retreat to the player's heel and idle —
    /// it WALKS to the current step's target station (the same one <see cref="ObjectiveMarker"/>
    /// beacons, resolved through the same <see cref="TutorialGuideStations"/>) and stands there,
    /// turned back to face the player, so its own body becomes a living waypoint: "go here" instead of
    /// a sentence you have to remember. Only a step with no place to point at
    /// (<see cref="TutorialGuideStation.None"/> — CreateCharacter, FirstDiscovery, FirstDeath) falls
    /// back to the old polite-follow-distance behaviour, because there is genuinely nowhere to send the
    /// player and standing still in an arbitrary spot would be worse than staying close.
    ///
    /// Procedural body (no authored art yet, same seam as MonsterBody/CityNpc) — its own hooded,
    /// lantern-carrying silhouette and warm gold palette, distinct from every monster and every other
    /// city NPC, so it reads as "not part of the world, here for YOU" at a glance.
    ///
    /// Self-installs at runtime (playtest, 2nd report: the guide only existed after a dev re-ran
    /// "Setup > Build All Scenes" — most players never do, so the whole authored first hour silently
    /// never appeared). Unlike CombatHud/ObjectiveMarker it must NOT be DontDestroyOnLoad — a fresh
    /// instance belongs to whichever scene the player is currently in (a different approach point and
    /// FollowPoint per scene), so it installs on EVERY scene load, not just the first, guarded so it
    /// never double-spawns alongside one ProjectAscensionSetup already baked into the committed scene
    /// (that editor-script path is kept working deliberately — see BuildAllScenes).
    /// </summary>
    public sealed class TutorialGuide : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // AfterSceneLoad only fires once, for the FIRST scene — City/Frontier reached later via
            // GameScenes.LoadCity/LoadFrontier need their own guide too, so every subsequent load is
            // covered by this subscription (registered here, before Bootstrap.Start() ever calls
            // SceneManager.LoadScene, so no transition can be missed).
            SceneManager.sceneLoaded += (scene, _) => EnsureForScene(scene.name);
            EnsureForScene(SceneManager.GetActiveScene().name);
        }

        private static void EnsureForScene(string sceneName)
        {
            // Where a fresh guide starts, beside the player's own spawn in that scene — mirrors
            // ProjectAscensionSetup's BuildCityScene/BuildFrontierScene placement exactly.
            Vector3 spawn;
            if (sceneName == GameScenes.City) spawn = CityBlockout.PlayerSpawn + new Vector3(2f, 0f, 1f);
            else if (sceneName == GameScenes.Frontier) spawn = new Vector3(2f, 0f, 1f);
            else return; // Bootstrap, or any scene outside the first hour: no guide belongs here

            // Guard: a scene rebuilt by ProjectAscensionSetup already has one baked in (its Awake has
            // already run by the time sceneLoaded fires) — never spawn a second.
            if (FindObjectOfType<TutorialGuide>() != null) return;

            var go = new GameObject("TutorialGuide");
            go.transform.position = spawn;
            go.AddComponent<TutorialGuide>();
        }

        private const string DisplayName = "Usher";

        private static readonly Color Robe = new Color(0.82f, 0.64f, 0.22f);
        private static readonly Color RobeDark = new Color(0.82f * 0.6f, 0.64f * 0.6f, 0.22f * 0.6f);
        private static readonly Color Trim = Color.Lerp(Robe, Color.white, 0.55f);
        private static readonly Color LanternGlow = new Color(1f, 0.85f, 0.45f);

        private const float MoveSpeed = 6.5f;
        private const float TurnDegreesPerSecond = 260f;
        private const float ArriveEpsilon = 0.15f;
        private const float LeaveDistance = 9f;

        // Idle: no station for this step (or none yet reachable) — hangs at a polite follow distance.
        // Approaching: walking up to the player to speak. Speaking: dialogue open.
        // Positioning: dismissed, walking to the current step's station (or, if there is none, to the
        // follow point — same rest as Idle, just named for what triggered it). Stationed: arrived at
        // the station and holding it, turned to face the player — the living waypoint.
        // Leaving/Gone: TutorialStep.Complete — walks offscreen for good.
        private enum GuideState { Idle, Approaching, Speaking, Positioning, Stationed, Leaving, Gone }

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
                case GuideState.Positioning:
                    MoveToward(PositioningTarget(), OnArrivedPositioning);
                    break;
                case GuideState.Stationed:
                    FacePlayer();
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
            _state = GuideState.Positioning;
        }

        // Walk to the step's station if it has one and it currently resolves in this scene; otherwise
        // fall back to the old polite follow point. Re-read every frame (not cached at CloseDialogue
        // time) so a station that becomes buildable mid-walk still gets picked up.
        private Vector3 PositioningTarget() =>
            HasStation(out var stationPos) ? stationPos : FollowPoint();

        private void OnArrivedPositioning() =>
            _state = HasStation(out _) ? GuideState.Stationed : GuideState.Idle;

        private bool HasStation(out Vector3 position)
        {
            if (_station != TutorialGuideStation.None && TutorialGuideStations.TryResolve(_station, out position))
                return true;
            position = default;
            return false;
        }

        private void FacePlayer()
        {
            if (_player != null) FaceDirection(_player.position - transform.position);
        }

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

        // A real dialogue, not a floating string (playtest, 2nd report): a framed panel with the
        // speaker's own name plate, the line, and an unmissable dismiss button — the ONLY thing the
        // guide's popup is allowed to look like is "someone is talking to you", never ambient text.
        private void OnGUI()
        {
            if (_state != GuideState.Speaking) return;

            // A light veil, not a blackout — you are standing in the open world, not at a station.
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            const float w = 640f, h = 148f, namePlateH = 30f, buttonW = 150f, buttonH = 30f;
            var box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.58f, w, h);

            // Panel body.
            GUI.color = new Color(0.08f, 0.07f, 0.05f, 0.92f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = prev;
            // Gold accent border so it reads as a FRAME, not a stray rectangle.
            DrawBorder(box, new Color(1f, 0.82f, 0.35f, 0.9f), 2f);

            // Name plate — its own strip along the top, not just a label floating in the body.
            var namePlate = new Rect(box.x, box.y, box.width, namePlateH);
            GUI.color = new Color(1f, 0.82f, 0.35f, 0.22f);
            GUI.DrawTexture(namePlate, Texture2D.whiteTexture);
            GUI.color = prev;
            var name = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };
            name.normal.textColor = new Color(1f, 0.88f, 0.55f);
            GUI.Label(namePlate, DisplayName, name);

            var say = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 17, wordWrap = true };
            say.normal.textColor = Color.white;
            GUI.Label(new Rect(box.x + 22f, namePlate.yMax + 12f, box.width - 44f, h - namePlateH - buttonH - 24f), _line, say);

            // The dismiss — a real button-looking pill, not a caption. Key label is read from the
            // live binding (PlayerInputHandler.InteractKeyLabel), never hardcoded "[F]".
            var button = new Rect(box.x + (box.width - buttonW) * 0.5f, box.yMax - buttonH - 12f, buttonW, buttonH);
            GUI.color = new Color(1f, 0.82f, 0.35f, 0.9f);
            GUI.DrawTexture(button, Texture2D.whiteTexture);
            GUI.color = prev;
            var buttonLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold };
            buttonLabel.normal.textColor = new Color(0.15f, 0.1f, 0.02f);
            GUI.Label(button, $"[{PlayerInputHandler.InteractKeyLabel}]  Continue", buttonLabel);
        }

        private static void DrawBorder(Rect r, Color color, float thickness)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), Texture2D.whiteTexture);
            GUI.color = prev;
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
