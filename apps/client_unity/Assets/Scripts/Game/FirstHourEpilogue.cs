using UnityEngine;
using ProjectAscension.GameSimulation.Tutorial;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Stage 11: the return. The first hour ends when the player walks back through the gate having
    /// fought, found something no one had, died, and learned they didn't have to do it alone.
    ///
    /// The doc's success criteria are THOUGHTS, not a UI ("저 계약은 누가 대신 해줄 수 있을까?", "이 세계는
    /// 생각보다 크다"), so this is deliberately not a checklist of systems cleared. It names what the
    /// player actually LIVED, in their own order, once — and then gets out of the way, because the point
    /// of the ending is that there isn't one.
    ///
    /// Shows once, when the director reaches Complete.
    /// </summary>
    public sealed class FirstHourEpilogue : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<FirstHourEpilogue>() != null) return;
            var go = new GameObject("FirstHourEpilogue");
            DontDestroyOnLoad(go);
            go.AddComponent<FirstHourEpilogue>();
        }

        private const float FadeIn = 1.2f;
        private const float Hold = 6.5f;
        private const float FadeOut = 1.8f;

        private static readonly string[] Lines =
        {
            "You went out past the wall, and you came back.",
            "You found something that was yours because of how you fought for it.",
            "You died, and the world did not stop to notice.",
            "And someone else finished what you could not.",
            "",
            "There is more out there than the map remembers.",
        };

        private bool _shown;
        private float _t = -1f;
        private Texture2D _tex;

        private void Awake()
        {
            _tex = new Texture2D(1, 1);
            _tex.SetPixel(0, 0, Color.white);
            _tex.Apply();
        }

        private void OnDestroy()
        {
            if (_tex != null) Destroy(_tex);
        }

        private void Update()
        {
            if (!_shown)
            {
                var runner = TutorialRunner.Instance;
                if (runner == null || runner.Progress.Step != TutorialStep.Complete) return;
                _shown = true;
                _t = 0f;
                return;
            }

            if (_t >= 0f)
            {
                _t += Time.unscaledDeltaTime;
                if (_t >= FadeIn + Hold + FadeOut) _t = -1f;
            }
        }

        private void OnGUI()
        {
            if (_t < 0f) return;

            float a = _t < FadeIn ? Mathf.Clamp01(_t / FadeIn)
                : _t < FadeIn + Hold ? 1f
                : Mathf.Clamp01(1f - (_t - FadeIn - Hold) / FadeOut);
            if (a <= 0f) return;

            var prev = GUI.color;

            // A veil, not a blackout — the city stays visible behind it. You are home; this is just a
            // moment to notice that you are.
            GUI.color = new Color(0.02f, 0.03f, 0.05f, 0.72f * a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _tex);

            float y = Screen.height * 0.34f;
            for (int i = 0; i < Lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(Lines[i]))
                {
                    bool last = i == Lines.Length - 1;
                    GUI.color = new Color(0.93f, 0.91f, 0.87f, a * (last ? 1f : 0.86f));
                    var style = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = last ? 20 : 16,
                        fontStyle = last ? FontStyle.Bold : FontStyle.Normal,
                        wordWrap = true,
                    };
                    style.normal.textColor = GUI.color;
                    GUI.Label(new Rect(0f, y, Screen.width, 34f), Lines[i], style);
                }
                y += 34f;
            }

            GUI.color = prev;
        }
    }
}
