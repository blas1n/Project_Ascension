using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAscension.Domain.Enums;
using ProjectAscension.GameSimulation.Combat;
using ProjectAscension.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The player's discovery journal — every discovered weapon, command, and passive, with how to
    /// use it and what it does. This is explicitly the PLAYER's own record, not a city fixture (it
    /// used to be bolted onto the contract board's panel): it opens on a HUD key (J) that works
    /// anywhere, city or frontier, not only near a station. Self-installs like TutorialRunner, so no
    /// scene wiring is needed and it survives the City&lt;-&gt;Frontier transition.
    /// </summary>
    public sealed class DiscoveryJournalHud : CityStationPanel
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<DiscoveryJournalHud>() != null) return;
            var go = new GameObject("DiscoveryJournalHud");
            DontDestroyOnLoad(go);
            go.AddComponent<DiscoveryJournalHud>();
        }

        private Vector2 _scroll;

        protected override void Update()
        {
            base.Update(); // Esc closes

            // Don't let J steal focus while some OTHER station panel already holds it (board,
            // equipment, quartermaster, clerk) — but always allow J to close the journal itself.
            if (UiFocus.IsFocused && !IsOpen) return;
            if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame) Toggle();
        }

        protected override void DrawPanel()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            GUILayout.BeginArea(new Rect((Screen.width - 460f) * 0.5f, 60f, 460f, 520f), GUI.skin.box);
            int discoveredCount = session.DiscoveredSkills.Weapons.Count
                + session.DiscoveredSkills.Commands.Count + session.DiscoveredSkills.Passives.Count;
            GUILayout.Label($"DISCOVERY JOURNAL ({discoveredCount})   —   [J] or [Esc] to close");
            GUILayout.Space(6);

            _scroll = GUILayout.BeginScrollView(_scroll);
            bool any = false;
            // Each discovered skill WITH how to use it (weapon/command hotkey/passive) and a short
            // EFFECT summary.
            foreach (var d in session.DiscoveredSkills.All)
            {
                string hint = d.Manifestation == ManifestationKind.Command
                    ? CommandHint(d, session)
                    : UseHint(d);
                GUILayout.Label($"• {d.Name}  [{hint}]");
                // The AI-composed description (a sentence, like a real game's skill text).
                string desc = !string.IsNullOrWhiteSpace(d.Description)
                    ? d.Description
                    : SkillSummary.Describe(d); // fallback if the model gave none (graph-derived)
                GUILayout.Label($"     {desc}");
                any = true;
            }
            if (!any)
                GUILayout.Label("None yet — fight and explore to discover.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // How the player uses a discovered skill.
        private static string UseHint(DiscoveredSkill d) => d.Manifestation switch
        {
            ManifestationKind.Weapon => "weapon: equip & fire",
            ManifestationKind.Passive => $"passive: {SkillSummary.DescribePassive(d)}",
            _ => "command",
        };

        // A command is cast from the ability hotkey the player bound it to; if it's weapon-bound it
        // also shows the equipment it needs (ADR 0005 재개정), so the player knows what to equip
        // before departing.
        private static string CommandHint(DiscoveredSkill d, GameSession session)
        {
            int slot = session.SlotOf(d);
            var required = CommandGate.RequiredEquipment(d);
            string reqTxt = required.Count > 0 ? $"  (needs {string.Join("/", required)})" : "";
            return slot >= 0 ? $"key [{AbilitySlots.SlotLabel(slot)}]{reqTxt}" : $"unassigned{reqTxt}";
        }
    }
}
