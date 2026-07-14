using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The armoury rack — docs/03-gameplay/first-hour-experience.md's stage 3 "첫 장비 선택" and
    /// Phase 7's "storage / equipment management" (CLAUDE.md). This is where you pick the two
    /// weapons that go in your hands, bind discovered commands to their hotkeys, see what's in
    /// storage, and save your progress. None of that is the contract board's or an NPC's business —
    /// it is a THING in the city you walk up to and use, same as the board. Departure itself is a
    /// separate PLACE (the frontier gate, CityBlockout.Gate / DepartZone) — not a button here.
    ///
    /// Every discovery — weapon or command — lands here inert (SkillCaster.OnSkillReady puts a
    /// weapon in the inventory, a command in the journal; neither auto-equips). This is the ONLY
    /// place either becomes usable: a button per hand/hotkey opens a scrollable picker of what can
    /// go there, you click an entry, it's bound. Commands are filtered to what's assignable with the
    /// CURRENT loadout (ADR 0011's equipment gate, via the pure GameSimulation.Combat.
    /// AssignableCommands) — no cyclers, so this scales past four commands.
    /// </summary>
    public sealed class EquipmentStationPanel : CityStationPanel
    {
        private enum Picker
        {
            None,
            WeaponLeft,
            WeaponRight,
            Command,
        }

        private Picker _picker = Picker.None;
        private int _commandSlotIndex = -1;
        private Vector2 _pickerScroll;

        private void Start()
        {
            if (CityBlockout.EquipmentInteractable != null)
                CityBlockout.EquipmentInteractable.Interacted += Toggle;

            // The tutorial's "첫 장비 선택" beat fires on the ACT of choosing a hand's weapon
            // (LoadoutChanged), never on merely opening this panel. LoadoutChanged is raised only
            // by SetLeft/SetRight — the pickers below — so a returning player's constructor-time
            // default loadout (which never calls them) can't auto-complete the step.
            var session = GameSession.Instance;
            if (session != null)
                session.PlayerState.LoadoutChanged += OnLoadoutChanged;
        }

        protected override void OnDestroy()
        {
            if (CityBlockout.EquipmentInteractable != null)
                CityBlockout.EquipmentInteractable.Interacted -= Toggle;
            var session = GameSession.Instance;
            if (session != null)
                session.PlayerState.LoadoutChanged -= OnLoadoutChanged;
            base.OnDestroy();
        }

        // Gated on both hands filled: a deliberate pick that leaves a hand empty is not yet "a
        // pair chosen" (matches the old button's hasFullLoadout gate).
        private void OnLoadoutChanged()
        {
            var ps = GameSession.Instance?.PlayerState;
            if (ps != null && ps.SelectedLeft != null && ps.SelectedRight != null)
                GameplayEvents.RaiseEquipmentChosen();
        }

        // A picker is a mode WITHIN this panel, not a separate one — Esc backs out of it first
        // (matching every other "step out" in this UI), and only a second Esc closes the station.
        protected override void Update()
        {
            if (IsOpen && _picker != Picker.None
                && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClosePicker();
                return;
            }
            base.Update();
        }

        protected override void OnOpened() => ClosePicker(); // never reopen mid-pick from a stale state

        protected override void DrawPanel()
        {
            var session = GameSession.Instance;
            if (session == null)
            {
                GUI.Label(new Rect(20, 20, 500, 20), "No GameSession — start play from the Bootstrap scene.");
                return;
            }

            if (_picker != Picker.None)
            {
                DrawPicker(session);
                return;
            }

            var ps = session.PlayerState;

            var o = ModalOrigin(420f, 560f);
            GUILayout.BeginArea(new Rect(o.x, o.y, 420f, 560f), GUI.skin.box);
            GUILayout.Label("EQUIPMENT STATION");
            GUILayout.Space(6);

            GUILayout.Label("Loadout (click a hand to choose from owned weapons):");
            // Left hand fires on RMB, right hand on LMB (PlayerCombat) — label it so the combo
            // guide's "LMB/RMB" is unambiguous.
            DrawWeaponSlotButton("Left (RMB) ", ps.SelectedLeft, Picker.WeaponLeft);
            DrawWeaponSlotButton("Right (LMB)", ps.SelectedRight, Picker.WeaponRight);

            // Ability bar: bind discovered commands to the Q/E/Shift/C hotkeys. Always shown, so the
            // player can see the slots even before discovering any command. Nothing auto-fills a
            // slot — a fresh discovery waits in the journal until bound here (see class doc). Same
            // combat lock as the journal's own binder (CommandBinderPicker) — a weapon rack in the
            // city will practically never see it engaged, but it is one rule, not two.
            GUILayout.Space(4);
            GUILayout.Label("Ability slots (click one to bind a command):");
            bool canRebind = CommandBinderPicker.CanRebindNow();
            if (!canRebind) GUILayout.Label($"  {CommandBinderPicker.LockedReason}");
            for (int i = 0; i < session.CommandSlots.Length; i++)
                DrawAbilitySlotButton(i, session, canRebind);

            GUILayout.Space(10);
            GUILayout.Label("Storage:");
            bool anyStored = false;
            foreach (var kv in ps.Resources)
            {
                GUILayout.Label($"  {kv.Key} x{kv.Value}  (material)");
                anyStored = true;
            }
            foreach (var kv in ps.Inventory.Owned)
            {
                GUILayout.Label($"  {kv.Key} x{kv.Value}  (item)");
                anyStored = true;
            }
            if (!anyStored)
                GUILayout.Label("  (empty)");

            // Departure is a place now (the frontier gate pad, CityBlockout), not a button in an open
            // panel — that used to unload the scene while this panel still held the UiFocus gate.
            GUILayout.Space(12);
            if (GUILayout.Button("Save", GUILayout.Height(34), GUILayout.Width(80)))
                session.Save();

            GUILayout.EndArea();
        }

        private void DrawWeaponSlotButton(string label, WeaponData current, Picker which)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(90));
            if (GUILayout.Button(current != null ? current.DisplayName : "(empty)", GUILayout.Height(26)))
                OpenPicker(which);
            GUILayout.EndHorizontal();
        }

        private void DrawAbilitySlotButton(int index, GameSession session, bool canRebind)
        {
            var current = session.CommandSlots[index];
            var required = current != null ? CommandGate.RequiredEquipment(current) : System.Array.Empty<string>();
            string req = required.Count > 0 ? "  needs " + string.Join("/", required) : "";
            string text = $"{(current != null ? current.Name : "(empty)")}{req}";

            GUILayout.BeginHorizontal();
            GUILayout.Label($"[{AbilitySlots.SlotLabel(index)}]", GUILayout.Width(50));
            GUI.enabled = canRebind;
            if (GUILayout.Button(text, GUILayout.Height(26)) && canRebind)
                OpenCommandPicker(index);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void OpenPicker(Picker which)
        {
            _picker = which;
            _commandSlotIndex = -1;
            _pickerScroll = Vector2.zero;
        }

        private void OpenCommandPicker(int slotIndex)
        {
            _picker = Picker.Command;
            _commandSlotIndex = slotIndex;
            _pickerScroll = Vector2.zero;
        }

        private void ClosePicker()
        {
            _picker = Picker.None;
            _commandSlotIndex = -1;
        }

        // The picker replaces the whole panel while open (simplest correct IMGUI layout for a list
        // that can scroll past the screen) — a scrollable list of entries, each one a click-to-bind
        // button, plus Cancel. Weapon hands list every owned weapon; hotkey slots list only the
        // commands ASSIGNABLE with the current loadout (AssignableCommands, ADR 0011) — the owner's
        // ask was "what can I bind with what I'm holding right now", not a longer list with reasons.
        private void DrawPicker(GameSession session)
        {
            var ps = session.PlayerState;
            var o = ModalOrigin(360f, 440f);
            GUILayout.BeginArea(new Rect(o.x, o.y, 360f, 440f), GUI.skin.box);
            GUILayout.Label(PickerTitle());
            GUILayout.Space(6);

            _pickerScroll = GUILayout.BeginScrollView(_pickerScroll, GUILayout.Height(340f));

            if (_picker == Picker.Command)
                DrawCommandPickerEntries(session, ps);
            else
                DrawWeaponPickerEntries(ps);

            GUILayout.EndScrollView();

            GUILayout.Space(6);
            if (GUILayout.Button("Cancel", GUILayout.Height(28)))
                ClosePicker();

            GUILayout.EndArea();
        }

        private void DrawWeaponPickerEntries(PlayerStateService ps)
        {
            if (ps.OwnedWeapons.Count == 0)
            {
                GUILayout.Label("  (no weapons owned)");
                return;
            }
            foreach (var w in ps.OwnedWeapons)
            {
                if (!GUILayout.Button(w.DisplayName, GUILayout.Height(28))) continue;
                if (_picker == Picker.WeaponLeft) ps.SetLeft(w); else ps.SetRight(w);
                ClosePicker();
                return;
            }
        }

        private void DrawCommandPickerEntries(GameSession session, PlayerStateService ps)
        {
            var equipped = CommandBinderPicker.CurrentEquipmentTags(ps);
            if (!CommandBinderPicker.DrawEntries(session, equipped, _commandSlotIndex, out var selection)) return;
            session.AssignCommandSlot(_commandSlotIndex, selection); // vacates any prior slot; null clears
            ClosePicker();
        }

        private string PickerTitle() => _picker switch
        {
            Picker.WeaponLeft => "Choose left-hand weapon (RMB)",
            Picker.WeaponRight => "Choose right-hand weapon (LMB)",
            Picker.Command => CommandBinderPicker.Title(_commandSlotIndex),
            _ => "",
        };
    }
}
