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
    ///
    /// It is also where a discovered COMMAND gets bound to a hotkey. Binding is knowledge, not
    /// equipment — a weapon is a physical object and stays at the Equipment Station, but a technique
    /// is something you carry in your head, so it binds anywhere. The only restriction is time, not
    /// place: binding locks for a short window after the player's own last combat activity
    /// (CommandBinderPicker.CanRebindNow) — you cannot re-sort your kit while a monster is chewing
    /// on you. The picker itself is EXTRACTED (CommandBinderPicker), shared with the Equipment
    /// Station's own ability bar, so the two can't drift.
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

        /// <summary>The journal's toggle key — a single source, so a rebind (or a future non-QWERTY
        /// default) only needs to change here.</summary>
        public const Key JournalKey = Key.J;

        /// <summary>Human-readable label for <see cref="JournalKey"/> (e.g. "J"), sourced from
        /// PlayerInputHandler.KeyLabel — DiscoveryNotification's toast reads this rather than
        /// hardcoding the letter.</summary>
        public static string JournalKeyLabel => PlayerInputHandler.KeyLabel(JournalKey);

        private Vector2 _scroll;
        private int _commandSlotIndex = -1; // >= 0 while the binder picker is open
        private Vector2 _pickerScroll;

        protected override void Update()
        {
            // A picker is a mode WITHIN this panel — Esc backs out of IT first (matching the
            // Equipment Station's own picker), and only a second Esc/J closes the journal.
            if (IsOpen && _commandSlotIndex >= 0
                && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClosePicker();
                return;
            }

            base.Update(); // Esc closes

            // Don't let J steal focus while some OTHER station panel already holds it (board,
            // equipment, quartermaster, clerk) — but always allow J to close the journal itself.
            if (UiFocus.IsFocused && !IsOpen) return;
            if (Keyboard.current != null && Keyboard.current[JournalKey].wasPressedThisFrame) Toggle();
        }

        protected override void OnOpened() => ClosePicker(); // never reopen mid-pick from a stale state

        protected override void DrawPanel()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            if (_commandSlotIndex >= 0)
            {
                DrawPicker(session);
                return;
            }

            GUILayout.BeginArea(new Rect((Screen.width - 460f) * 0.5f, 60f, 460f, 560f), GUI.skin.box);
            int discoveredCount = session.DiscoveredSkills.Weapons.Count
                + session.DiscoveredSkills.Commands.Count + session.DiscoveredSkills.Passives.Count;
            GUILayout.Label($"DISCOVERY JOURNAL ({discoveredCount})   —   [{JournalKeyLabel}] or [Esc] to close");
            GUILayout.Space(6);

            DrawBinder(session);

            GUILayout.Space(10);
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

        // Bind discovered commands to the Q/E/Shift/C hotkeys — the same ability bar the Equipment
        // Station shows, so a command found mid-expedition is usable the moment it's bound, not
        // after a walk back to the city (the whole point of a discovery is using the thing you just
        // made). Locked for a short window after combat (CommandBinderPicker.CanRebindNow) — you
        // may bind anywhere, just not mid-fight.
        private void DrawBinder(GameSession session)
        {
            GUILayout.Label("Ability slots (click one to bind a command):");
            bool canRebind = CommandBinderPicker.CanRebindNow();
            if (!canRebind) GUILayout.Label($"  {CommandBinderPicker.LockedReason}");
            for (int i = 0; i < session.CommandSlots.Length; i++)
                DrawSlotButton(i, session, canRebind);
        }

        private void DrawSlotButton(int index, GameSession session, bool canRebind)
        {
            var current = session.CommandSlots[index];
            var required = current != null ? CommandGate.RequiredEquipment(current) : System.Array.Empty<string>();
            string req = required.Count > 0 ? "  needs " + string.Join("/", required) : "";
            string text = $"{(current != null ? current.Name : "(empty)")}{req}";

            GUILayout.BeginHorizontal();
            GUILayout.Label($"[{AbilitySlots.SlotLabel(index)}]", GUILayout.Width(50));
            GUI.enabled = canRebind;
            if (GUILayout.Button(text, GUILayout.Height(26)) && canRebind)
                OpenPicker(index);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void OpenPicker(int slotIndex)
        {
            _commandSlotIndex = slotIndex;
            _pickerScroll = Vector2.zero;
        }

        private void ClosePicker() => _commandSlotIndex = -1;

        private void DrawPicker(GameSession session)
        {
            var o = ModalOrigin(360f, 440f);
            GUILayout.BeginArea(new Rect(o.x, o.y, 360f, 440f), GUI.skin.box);
            GUILayout.Label(CommandBinderPicker.Title(_commandSlotIndex));
            GUILayout.Space(6);

            _pickerScroll = GUILayout.BeginScrollView(_pickerScroll, GUILayout.Height(340f));
            var equipped = CommandBinderPicker.CurrentEquipmentTags(session.PlayerState);
            if (CommandBinderPicker.DrawEntries(session, equipped, _commandSlotIndex, out var selection))
            {
                session.AssignCommandSlot(_commandSlotIndex, selection); // vacates any prior slot; null clears
                ClosePicker();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            if (GUILayout.Button("Cancel", GUILayout.Height(28)))
                ClosePicker();

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
