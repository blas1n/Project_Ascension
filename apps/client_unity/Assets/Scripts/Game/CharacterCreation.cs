using System.Collections;
using UnityEngine;
using ProjectAscension.GameSimulation.Tutorial;
using ProjectAscension.Net;
using ProjectAscension.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Stage 0: you make someone (docs/03-gameplay/first-hour-experience.md — "기본 외형과 이름을 설정할
    /// 수 있다"). It is deliberately small. This is not a character builder with a hundred sliders; it is
    /// the moment you stop being a camera and start being a person who can die.
    ///
    /// It is also the last of the first hour's temporary scaffolding: until now the tutorial auto-passed
    /// this step because there was nothing to pass. Now the player names themselves, and that name is
    /// what the world will use.
    ///
    /// Self-installs; shows only while the director is on CreateCharacter, so a returning player never
    /// sees it again.
    /// </summary>
    public sealed class CharacterCreation : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<CharacterCreation>() != null) return;
            var go = new GameObject("CharacterCreation");
            DontDestroyOnLoad(go);
            go.AddComponent<CharacterCreation>();
        }

        private static readonly (string Name, Color Color)[] Looks =
        {
            ("Ash", new Color(0.72f, 0.72f, 0.74f)),
            ("Ember", new Color(0.78f, 0.45f, 0.32f)),
            ("Moss", new Color(0.45f, 0.62f, 0.44f)),
            ("Tide", new Color(0.42f, 0.58f, 0.75f)),
        };

        private string _name = "";
        private int _look;
        private bool _done;
        private bool _focusHeld; // whether WE currently hold the UiFocus gate (Push/Pop exactly once)

        private bool Active =>
            !_done && TutorialRunner.Instance != null &&
            TutorialRunner.Instance.Progress.Step == TutorialStep.CreateCharacter;

        private void Update()
        {
            // BUG 3: typing the name must not also drive the player. UiFocus disables the "Player"
            // action map (+ AbilitySlots) and owns the cursor while this form is up, restoring
            // whatever state (lock mode, visibility) preceded it once we Pop.
            if (Active && !_focusHeld) { UiFocus.Push(); _focusHeld = true; }
            else if (!Active && _focusHeld) { UiFocus.Pop(); _focusHeld = false; }
        }

        private void OnDestroy()
        {
            // Defensive: this object is DontDestroyOnLoad and normally outlives the form, but an
            // unmatched Push would permanently disable gameplay input if it were ever destroyed
            // while still holding focus (e.g. a domain reload mid-play in the editor).
            if (_focusHeld) { UiFocus.Pop(); _focusHeld = false; }
        }

        private void OnGUI()
        {
            if (!Active) return;

            // Behind the form, the world waits.
            var dim = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = dim;

            const float w = 460f, h = 250f;
            var box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            GUILayout.BeginArea(box, GUI.skin.box);
            GUILayout.Space(8f);

            var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            GUILayout.Label("Who are you?", title);
            GUILayout.Space(10f);

            GUILayout.Label("Name");
            GUI.SetNextControlName("name");
            _name = GUILayout.TextField(_name ?? "", 20);

            GUILayout.Space(10f);
            GUILayout.Label("Bearing");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Looks.Length; i++)
            {
                var was = GUI.backgroundColor;
                GUI.backgroundColor = Looks[i].Color;
                if (GUILayout.Toggle(_look == i, Looks[i].Name, GUI.skin.button, GUILayout.Height(28f))) _look = i;
                GUI.backgroundColor = was;
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            bool named = !string.IsNullOrWhiteSpace(_name);
            GUI.enabled = named;
            if (GUILayout.Button(named ? "Step outside" : "Give yourself a name", GUILayout.Height(34f)))
                Confirm();
            GUI.enabled = true;

            GUILayout.Space(6f);
            GUILayout.EndArea();
        }

        private void Confirm()
        {
            _done = true;

            var session = GameSession.Instance;
            if (session != null && session.PlayerState != null)
                session.PlayerState.CharacterName = _name.Trim();

            ApplyLook();

            // The world takes the cursor (and gameplay input) back — restores whatever state
            // preceded the form, same gate the city panels use (UiFocus). Popped here rather than
            // waiting for the next Update() so there is no one-frame gap.
            if (_focusHeld) { UiFocus.Pop(); _focusHeld = false; }

            // This is the ONLY place a client identity is minted (GameSession must never assume
            // one — a fresh database has no Actor row until this call creates it). A returning
            // player already has an actor id restored from PlayerPrefs at session start, so there
            // is nothing to create — just move on. Offline has no server to ask, so it moves on too.
            bool needsIdentity = session != null && string.IsNullOrWhiteSpace(session.ActorId)
                && !string.IsNullOrWhiteSpace(session.ServerUrl);
            if (needsIdentity)
                StartCoroutine(CreateCharacterOnServer(session));
            else
                TutorialRunner.Instance.Signal(TutorialSignal.NameChosen);
        }

        private IEnumerator CreateCharacterOnServer(GameSession session)
        {
            var api = new CatalogApiClient(session.ServerUrl);
            yield return api.CreateCharacter(_name.Trim(), dto =>
            {
                if (dto != null && !string.IsNullOrEmpty(dto.actorId)) session.SetActorId(dto.actorId);
                else Debug.LogError("[CharacterCreation] Character creation returned no actor id — discovery/contracts will not work this session.");
            },
            error => Debug.LogError($"[CharacterCreation] Character creation failed: {CatalogApiClient.ParseErrorMessage(error)}"));

            // The tutorial advances regardless of the outcome — the player is already back in the
            // world by the time the response lands; a failed creation is logged, not blocking.
            TutorialRunner.Instance.Signal(TutorialSignal.NameChosen);
        }

        /// <summary>Appearance is a colour for now — an FPS rarely shows you your own face, and the art
        /// track will put a real body in this seam. It still matters that you CHOSE it.</summary>
        private void ApplyLook()
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo == null) return;

            var body = playerGo.transform.Find("Body");
            var renderer = body != null ? body.GetComponent<Renderer>() : playerGo.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material.color = Looks[_look].Color;
        }
    }
}
