using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The first death, staged (docs/03-gameplay/first-hour-experience.md, stage 8: "사망은 연출된
    /// 경험이다"). Dying was a Debug.Log and a teleport — the single most important beat in the first
    /// hour happened silently and in place.
    ///
    /// Now it lands: the world goes still and dark, it says the one thing it needs to say, and then you
    /// wake up in the city. Waking up HOME is the whole design — the contract is still open, you still
    /// cannot finish it, and the city is where the answer to that lives (위임, then 발주). The death
    /// doesn't punish you; it hands you to the next stage.
    ///
    /// Self-installs, survives the scene change, and only presents — the failure rules (which contracts
    /// die with you, and the delegation hint) are GameSession's.
    /// </summary>
    public sealed class DeathSequence : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<DeathSequence>() != null) return;
            var go = new GameObject("DeathSequence");
            DontDestroyOnLoad(go);
            go.AddComponent<DeathSequence>();
        }

        private const float FadeInSeconds = 0.9f;   // the world going out
        private const float HoldSeconds = 2.6f;     // long enough to sit with it
        private const float FadeOutSeconds = 0.8f;  // waking up

        private static readonly string[] Lines =
        {
            "You died.",
            "The frontier does not grade you on effort.",
        };

        private Texture2D _black;
        private float _t = -1f;   // <0 = not dying
        private bool _returned;

        private void Awake()
        {
            _black = new Texture2D(1, 1);
            _black.SetPixel(0, 0, Color.white);
            _black.Apply();
            GameplayEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDestroy()
        {
            GameplayEvents.PlayerDied -= OnPlayerDied;
            if (_black != null) Destroy(_black);
        }

        private void OnPlayerDied()
        {
            if (_t >= 0f) return; // already dying — a corpse cannot die twice
            _t = 0f;
            _returned = false;
        }

        private void Update()
        {
            if (_t < 0f) return;

            _t += Time.unscaledDeltaTime;

            // Wake up in the city, under the black, so the transition is never seen.
            if (!_returned && _t >= FadeInSeconds + HoldSeconds * 0.5f)
            {
                _returned = true;
                GameScenes.LoadCity();
            }

            if (_t >= FadeInSeconds + HoldSeconds + FadeOutSeconds) _t = -1f; // done
        }

        private void OnGUI()
        {
            if (_t < 0f) return;

            float alpha = Alpha(_t);
            if (alpha <= 0f) return;

            var prev = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _black);

            // The words only while the dark is full — they shouldn't compete with the fade.
            float text = Mathf.Clamp01((_t - FadeInSeconds) / 0.4f) *
                         Mathf.Clamp01((FadeInSeconds + HoldSeconds - _t) / 0.4f);
            if (text > 0f)
            {
                for (int i = 0; i < Lines.Length; i++)
                {
                    GUI.color = new Color(0.9f, 0.88f, 0.86f, text * (i == 0 ? 1f : 0.75f));
                    var style = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = i == 0 ? 30 : 15,
                        fontStyle = i == 0 ? FontStyle.Bold : FontStyle.Normal,
                    };
                    style.normal.textColor = GUI.color;
                    GUI.Label(new Rect(0f, Screen.height * 0.42f + i * 46f, Screen.width, 40f), Lines[i], style);
                }
            }

            GUI.color = prev;
        }

        private static float Alpha(float t)
        {
            if (t < FadeInSeconds) return Mathf.Clamp01(t / FadeInSeconds);
            if (t < FadeInSeconds + HoldSeconds) return 1f;
            return Mathf.Clamp01(1f - (t - FadeInSeconds - HoldSeconds) / FadeOutSeconds);
        }
    }
}
