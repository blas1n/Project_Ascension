using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Player combat-feedback HUD — health bar, crosshair, a red flash on taking damage, and a cyan
    /// flash when a dodge's i-frames negate a hit. OnGUI placeholder (a uGUI migration is a later
    /// track); this delivers the FEEL now. Self-installs at runtime and binds to the player's
    /// <see cref="HitReceiver"/>, drawing nothing when there is no player (city/menu). Rendering only
    /// — health, damage, and i-frames are owned by GameSimulation + HitReceiver.
    /// </summary>
    public sealed class CombatHud : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<CombatHud>() != null) return;
            var go = new GameObject("CombatHud");
            DontDestroyOnLoad(go); // survives city<->frontier; self-guards drawing by player presence
            go.AddComponent<CombatHud>();
        }

        private const float FlashDuration = 0.35f;

        private HitReceiver _player;
        private float _damageFlash; // 1 → 0 red on taking damage
        private float _negateFlash; // 1 → 0 cyan on an i-frame negate
        private Texture2D _tex;

        private void Awake()
        {
            _tex = new Texture2D(1, 1);
            _tex.SetPixel(0, 0, Color.white);
            _tex.Apply();
        }

        private void OnDestroy()
        {
            Unbind();
            if (_tex != null) Destroy(_tex);
        }

        private void Update()
        {
            // (Re)bind to the current player — it may not exist yet, or may have been rebuilt on a
            // scene change. Unity's fake-null makes a destroyed receiver compare == null.
            if (_player == null)
            {
                var playerGo = GameObject.FindWithTag("Player");
                if (playerGo != null && playerGo.TryGetComponent<HitReceiver>(out var hr))
                    Bind(hr);
            }

            float dt = Time.unscaledDeltaTime; // fade even during hit-stop
            _damageFlash = Mathf.MoveTowards(_damageFlash, 0f, dt / FlashDuration);
            _negateFlash = Mathf.MoveTowards(_negateFlash, 0f, dt / FlashDuration);
        }

        private void Bind(HitReceiver hr)
        {
            Unbind();
            _player = hr;
            _player.Damaged += OnDamaged;
            _player.DamageNegated += OnNegated;
        }

        private void Unbind()
        {
            if (_player == null) return;
            _player.Damaged -= OnDamaged;
            _player.DamageNegated -= OnNegated;
            _player = null;
        }

        private void OnDamaged(HitReceiver _, float __) => _damageFlash = 1f;
        private void OnNegated(HitReceiver _) => _negateFlash = 1f;

        private void OnGUI()
        {
            // Full-screen feedback flashes (red = hit, cyan = i-frame negate). Drawn even with no
            // player so a negate right as a scene ends still reads.
            if (_damageFlash > 0f) DrawFullscreen(new Color(0.8f, 0.05f, 0.05f, 0.45f * _damageFlash));
            if (_negateFlash > 0f) DrawFullscreen(new Color(0.4f, 0.85f, 1f, 0.5f * _negateFlash));

            if (_player == null) return;

            DrawCrosshair();
            DrawHealthBar();
        }

        private void DrawFullscreen(Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _tex);
            GUI.color = prev;
        }

        private void DrawCrosshair()
        {
            const float size = 10f, thick = 2f, gap = 4f;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            // Four ticks around a centre gap — a simple, readable reticle.
            GUI.DrawTexture(new Rect(cx - gap - size, cy - thick * 0.5f, size, thick), _tex); // left
            GUI.DrawTexture(new Rect(cx + gap, cy - thick * 0.5f, size, thick), _tex);        // right
            GUI.DrawTexture(new Rect(cx - thick * 0.5f, cy - gap - size, thick, size), _tex); // up
            GUI.DrawTexture(new Rect(cx - thick * 0.5f, cy + gap, thick, size), _tex);        // down
            GUI.color = prev;
        }

        private void DrawHealthBar()
        {
            const float w = 320f, h = 18f, pad = 24f;
            float x = (Screen.width - w) * 0.5f, y = Screen.height - h - pad;
            float frac = _player.Max > 0f ? Mathf.Clamp01(_player.Current / _player.Max) : 0f;

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);                        // backing
            GUI.DrawTexture(new Rect(x - 2, y - 2, w + 4, h + 4), _tex);
            GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);               // empty track
            GUI.DrawTexture(new Rect(x, y, w, h), _tex);
            // Green→red as health drops, so the state reads at a glance.
            GUI.color = Color.Lerp(new Color(0.8f, 0.15f, 0.15f), new Color(0.3f, 0.85f, 0.35f), frac);
            GUI.DrawTexture(new Rect(x, y, w * frac, h), _tex);
            GUI.color = prev;

            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(x, y, w, h), $"{Mathf.CeilToInt(_player.Current)} / {Mathf.CeilToInt(_player.Max)}", style);
        }
    }
}
