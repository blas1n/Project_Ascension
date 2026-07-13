using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;
using ProjectAscension.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Player combat-feedback HUD — health bar, crosshair, and a flash that tells you WHAT happened to
    /// an incoming blow: red = it landed, pale steel = a raised shield absorbed it. OnGUI placeholder
    /// (a uGUI migration is a later track); this delivers the FEEL now. Self-installs at runtime and
    /// binds to the player's <see cref="HitReceiver"/>, drawing nothing when there is no player
    /// (city/menu). Rendering only — health, damage, and blocking are owned by GameSimulation +
    /// HitReceiver.
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
        private Loadout _loadout; // read-only: which equipped weapon (if any) has a magazine to show
        private float _damageFlash; // 1 → 0 red on taking damage
        private float _blockFlash;  // 1 → 0 pale on a shield absorbing a blow
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
                {
                    Bind(hr);
                    playerGo.TryGetComponent(out _loadout); // may be null offline; magazine draw just no-ops
                }
            }

            float dt = Time.unscaledDeltaTime; // fade even during hit-stop
            _damageFlash = Mathf.MoveTowards(_damageFlash, 0f, dt / FlashDuration);
            _blockFlash = Mathf.MoveTowards(_blockFlash, 0f, dt / FlashDuration);
        }

        private void Bind(HitReceiver hr)
        {
            Unbind();
            _player = hr;
            _player.Damaged += OnDamaged;
            _player.DamageBlocked += OnBlocked;
        }

        private void Unbind()
        {
            if (_player == null) return;
            _player.Damaged -= OnDamaged;
            _player.DamageBlocked -= OnBlocked;
            _player = null;
            _loadout = null;
        }

        private void OnDamaged(HitReceiver _, float __) => _damageFlash = 1f;
        private void OnBlocked(HitReceiver _) => _blockFlash = 1f;

        private void OnGUI()
        {
            // Full-screen feedback flashes (red = hit, pale steel = blocked). Drawn even with no
            // player so a flash right as a scene ends still reads.
            if (_damageFlash > 0f) DrawFullscreen(new Color(0.8f, 0.05f, 0.05f, 0.45f * _damageFlash));
            if (_blockFlash > 0f) DrawFullscreen(new Color(0.85f, 0.88f, 0.95f, 0.35f * _blockFlash));

            if (_player == null) return;

            DrawCrosshair();
            DrawHealthBar();
            DrawMagazine();
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

        // Ammo readout, BOTTOM-RIGHT (the conventional FPS position) — clear of the health bar
        // (bottom-center), the contract HUD/focus/gold (top-left), and SkillGuideHud (top-right;
        // see its own comment about this hand-coordinated layout). The player has TWO hands and
        // BOTH can hold a firearm (e.g. dual pistols), so both magazines show when both are
        // present, and NEITHER draws when neither hand has one (sword/bow-only/catalyst/shield).
        // Reloading shows progress instead of a count; empty and idle hints the reload key rather
        // than leaving the player clicking a dead trigger unexplained.
        private void DrawMagazine()
        {
            if (_loadout == null) return;
            var right = _loadout.RightSlot?.Current as WeaponBase;   // LMB — primary fire
            var left = _loadout.LeftSlot?.Current as WeaponBase;     // RMB
            bool rightHas = right != null && right.HasMagazine;
            bool leftHas = left != null && left.HasMagazine;
            if (!rightHas && !leftHas) return;

            // LMB sits closest to the corner (primary fire, the conventional single-readout spot);
            // RMB stacks above it only when the other hand also holds a magazine weapon.
            float y = Screen.height - MagazinePad;
            if (rightHas) y = DrawMagazineRow(y, "LMB", right);
            if (leftHas) DrawMagazineRow(y, "RMB", left);
        }

        private const float MagazinePad = 24f, MagazineWidth = 170f, MagazineRowHeight = 20f,
            MagazineBarHeight = 5f, MagazineGap = 4f;

        // Draws one hand's ammo readout with its BOTTOM edge at `bottom` (growing upward) and
        // returns the y to stack the next row above it.
        private float DrawMagazineRow(float bottom, string label, WeaponBase weapon)
        {
            float x = Screen.width - MagazineWidth - MagazinePad;
            float rowY = bottom - MagazineRowHeight;
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            var prev = GUI.color;

            if (weapon.IsReloading)
            {
                GUI.color = new Color(1f, 0.85f, 0.3f, 0.95f);
                GUI.Label(new Rect(x, rowY, MagazineWidth, MagazineRowHeight), $"{label}  Reloading…", style);
                GUI.color = prev;
                float barY = rowY - MagazineGap - MagazineBarHeight;
                DrawReloadBar(x, barY, MagazineWidth, weapon.ReloadFraction);
                return barY - MagazineGap;
            }

            GUI.color = weapon.Loaded <= 0 ? new Color(0.9f, 0.3f, 0.25f, 0.95f) : new Color(1f, 1f, 1f, 0.85f);
            string text = weapon.Loaded <= 0
                ? $"{label}  Reload [{PlayerInputHandler.ReloadKeyLabel}]"
                : $"{label}  {weapon.Loaded} / {weapon.MagazineSize}";
            GUI.Label(new Rect(x, rowY, MagazineWidth, MagazineRowHeight), text, style);
            GUI.color = prev;
            return rowY - MagazineGap;
        }

        private void DrawReloadBar(float x, float y, float w, float frac)
        {
            const float h = MagazineBarHeight;
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), _tex);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, w, h), _tex);
            GUI.color = new Color(1f, 0.85f, 0.3f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(frac), h), _tex);
            GUI.color = prev;
        }
    }
}
