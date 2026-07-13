using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Combat;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// The armoury rack — docs/03-gameplay/first-hour-experience.md's stage 3 "첫 장비 선택" and
    /// Phase 7's "storage / equipment management" (CLAUDE.md). This is where you pick the two
    /// weapons that go in your hands, bind discovered commands to their hotkeys, see what's in
    /// storage, and (since it's the last stop before heading out) save and depart. None of that is
    /// the contract board's or an NPC's business — it is a THING in the city you walk up to and use,
    /// same as the board.
    /// </summary>
    public sealed class EquipmentStationPanel : CityStationPanel
    {
        private void Start()
        {
            if (CityBlockout.EquipmentInteractable != null)
                CityBlockout.EquipmentInteractable.Interacted += Toggle;
        }

        protected override void OnDestroy()
        {
            if (CityBlockout.EquipmentInteractable != null)
                CityBlockout.EquipmentInteractable.Interacted -= Toggle;
            base.OnDestroy();
        }

        protected override void DrawPanel()
        {
            var session = GameSession.Instance;
            if (session == null)
            {
                GUI.Label(new Rect(20, 20, 500, 20), "No GameSession — start play from the Bootstrap scene.");
                return;
            }
            var ps = session.PlayerState;

            var o = ModalOrigin(420f, 560f);
            GUILayout.BeginArea(new Rect(o.x, o.y, 420f, 560f), GUI.skin.box);
            GUILayout.Label("EQUIPMENT STATION");
            GUILayout.Space(6);

            GUILayout.Label("Loadout (chosen from inventory):");
            // Left hand fires on RMB, right hand on LMB (PlayerCombat) — label it so the combo
            // guide's "LMB/RMB" is unambiguous.
            DrawWeaponSelector("Left (RMB) ", ps.SelectedLeft, ps.SetLeft, ps.OwnedWeapons);
            DrawWeaponSelector("Right (LMB)", ps.SelectedRight, ps.SetRight, ps.OwnedWeapons);

            // The tutorial's "첫 장비 선택" beat fires on this explicit commit, never on merely
            // opening the panel — browsing the < > cyclers alone must not advance the first hour.
            // Gated on both hands filled so it also can't fire on a half-empty loadout; a returning
            // player who already has both hands filled can just press it again to satisfy the step.
            bool hasFullLoadout = ps.SelectedLeft != null && ps.SelectedRight != null;
            GUI.enabled = hasFullLoadout;
            if (GUILayout.Button("Confirm Loadout", GUILayout.Height(28)))
                GameplayEvents.RaiseEquipmentChosen();
            GUI.enabled = true;

            // Ability bar: bind discovered commands to the Q/E/Shift/C hotkeys. Always shown, so the
            // player can see the slots even before discovering any command.
            var commands = session.DiscoveredSkills.Commands;
            session.EnsureDefaultCommandSlots();
            GUILayout.Space(4);
            GUILayout.Label("Ability slots (hotkeys):");
            if (commands.Count == 0)
                GUILayout.Label("  (none yet — discover a non-magic combat skill)");
            else
                for (int i = 0; i < session.CommandSlots.Length; i++)
                    DrawAbilitySlot(i, session, commands);

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

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Height(34), GUILayout.Width(80)))
                session.Save();
            if (GUILayout.Button("Depart to Frontier", GUILayout.Height(34)))
            {
                session.Save(); // persist progress before leaving
                GameScenes.LoadFrontier();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private static void DrawWeaponSelector(string label, WeaponData current,
            System.Action<WeaponData> set, IReadOnlyList<WeaponData> owned)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {(current != null ? current.DisplayName : "-")}", GUILayout.Width(170));
            if (GUILayout.Button("<", GUILayout.Width(30))) set(Cycle(owned, current, -1));
            if (GUILayout.Button(">", GUILayout.Width(30))) set(Cycle(owned, current, +1));
            GUILayout.EndHorizontal();
        }

        private static WeaponData Cycle(IReadOnlyList<WeaponData> owned, WeaponData current, int dir)
        {
            if (owned.Count == 0) return current;
            int index = 0;
            for (int i = 0; i < owned.Count; i++)
                if (owned[i] == current) { index = i; break; }
            index = (index + dir + owned.Count) % owned.Count;
            return owned[index];
        }

        private static void DrawAbilitySlot(int index, GameSession session, IReadOnlyList<DiscoveredSkill> commands)
        {
            var current = session.CommandSlots[index];
            var required = current != null ? CommandGate.RequiredEquipment(current) : System.Array.Empty<string>();
            string req = required.Count > 0 ? "  needs " + string.Join("/", required) : "";
            GUILayout.BeginHorizontal();
            GUILayout.Label($"[{AbilitySlots.SlotLabel(index)}] {(current != null ? current.Name : "(none)")}{req}", GUILayout.Width(260));
            if (GUILayout.Button("<", GUILayout.Width(30))) session.AssignCommandSlot(index, CycleCommand(commands, current, -1));
            if (GUILayout.Button(">", GUILayout.Width(30))) session.AssignCommandSlot(index, CycleCommand(commands, current, +1));
            GUILayout.EndHorizontal();
        }

        // Cycle through the commands plus a "(none)" entry at position 0.
        private static DiscoveredSkill CycleCommand(IReadOnlyList<DiscoveredSkill> commands, DiscoveredSkill current, int dir)
        {
            int n = commands.Count + 1;
            int idx = 0; // 0 = none
            for (int i = 0; i < commands.Count; i++)
                if (ReferenceEquals(commands[i], current)) { idx = i + 1; break; }
            idx = (idx + dir + n) % n;
            return idx == 0 ? null : commands[idx - 1];
        }
    }
}
