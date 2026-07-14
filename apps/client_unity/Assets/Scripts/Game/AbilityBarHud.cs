using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Overwatch-style ability bar — the four <see cref="AbilitySlots"/> hotkeys as a row of
    /// framed boxes: key + skill name, greying out with a vertical cooldown sweep + the
    /// remaining seconds while cooling, bright and legible the instant it's ready. Replaces the
    /// old text list (SkillGuideHud's former "ABILITIES" section) with something readable at a
    /// glance mid-fight — the project-owner decision that removed the Focus resource asked for
    /// this UI shape specifically.
    ///
    /// Bottom-centre, stacked directly above CombatHud's health bar (also bottom-centre) and
    /// centered horizontally, so it stays clear of CombatHud's bottom-RIGHT magazine readout at
    /// any reasonable resolution without needing to know its exact width. Mirrors CombatHud's
    /// health-bar geometry (h=18, pad=24) so the two rows read as one HUD cluster. OnGUI
    /// placeholder like the rest of the combat HUD (a uGUI migration is a later track).
    /// </summary>
    [RequireComponent(typeof(AbilitySlots))]
    public sealed class AbilityBarHud : MonoBehaviour
    {
        private const float BoxSize = 64f, Gap = 10f, GapAboveHealthBar = 14f;
        // Mirrors CombatHud.DrawHealthBar's constants — kept in sync by hand (small, stable values).
        private const float HealthBarHeight = 18f, HealthBarPad = 24f;

        private AbilitySlots _slots;
        private Loadout _loadout;
        private Texture2D _tex;

        private void Awake()
        {
            _slots = GetComponent<AbilitySlots>();
            _tex = new Texture2D(1, 1);
            _tex.SetPixel(0, 0, Color.white);
            _tex.Apply();
        }

        private void OnDestroy()
        {
            if (_tex != null) Destroy(_tex);
        }

        private void OnGUI()
        {
            var session = GameSession.Instance;
            if (session == null || _slots == null) return;
            var slots = session.CommandSlots;

            if (_loadout == null) _loadout = FindAnyObjectByType<Loadout>();
            var current = _loadout != null ? EquipmentTags.CurrentTags(_loadout) : new HashSet<string>();

            float totalWidth = AbilitySlots.SlotCount * BoxSize + (AbilitySlots.SlotCount - 1) * Gap;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float bottom = Screen.height - HealthBarPad - HealthBarHeight - GapAboveHealthBar;
            float y = bottom - BoxSize;

            for (int i = 0; i < AbilitySlots.SlotCount; i++)
            {
                float x = startX + i * (BoxSize + Gap);
                var command = slots != null && i < slots.Length ? slots[i] : null;
                DrawSlot(new Rect(x, y, BoxSize, BoxSize), i, command, current);
            }
        }

        private void DrawSlot(Rect rect, int index, DiscoveredSkill command, HashSet<string> current)
        {
            var prev = GUI.color;

            // Frame — always drawn, even for an empty slot, so it reads as EMPTY, not broken.
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), _tex);

            bool locked = command != null && !CommandGate.Invocable(command, current);
            float remaining = command != null ? _slots.CooldownRemaining(index) : 0f;
            float fraction = command != null ? _slots.CooldownFraction(index) : 0f;
            bool cooling = remaining > 0.01f;

            GUI.color = command == null ? new Color(0.08f, 0.08f, 0.08f, 0.5f)
                : locked ? new Color(0.18f, 0.09f, 0.09f, 0.85f)
                : cooling ? new Color(0.12f, 0.12f, 0.12f, 0.85f)
                : new Color(0.16f, 0.19f, 0.24f, 0.92f); // ready: brighter steel-blue backing
            GUI.DrawTexture(rect, _tex);

            // Cooldown sweep: a dark cover shrinking from the top down as the wait finishes, so
            // remaining time reads as remaining AREA at a glance (Overwatch's radial, flattened to
            // IMGUI-friendly vertical fill).
            if (cooling)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.65f);
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, rect.height * fraction), _tex);
            }

            GUI.color = prev;

            var keyStyle = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, fontSize = 14 };
            GUI.color = command == null ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            GUI.Label(new Rect(rect.x, rect.y + 2f, rect.width, 18f), AbilitySlots.SlotLabel(index) ?? "-", keyStyle);
            GUI.color = prev;

            if (command == null) return;

            if (cooling)
            {
                var cdStyle = new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 };
                GUI.color = new Color(1f, 1f, 1f, 0.95f);
                GUI.Label(rect, $"{remaining:F1}", cdStyle);
                GUI.color = prev;
            }
            else
            {
                var nameStyle = new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.LowerCenter, fontSize = 10, wordWrap = true };
                GUI.color = locked ? new Color(1f, 0.55f, 0.35f, 0.9f) : new Color(1f, 1f, 1f, 0.9f);
                GUI.Label(new Rect(rect.x + 2f, rect.y + rect.height - 22f, rect.width - 4f, 20f), command.Name, nameStyle);
                GUI.color = prev;
            }
        }
    }
}
