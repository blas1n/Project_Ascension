using System.Collections.Generic;
using UnityEngine;
using ProjectAscension.Equipment;

namespace ProjectAscension.Game
{
    /// <summary>
    /// City hub UI (dev OnGUI): contract board, loadout selection, turn-in, depart.
    /// Cursor is unlocked here so the buttons are clickable.
    /// </summary>
    public sealed class CityHub : MonoBehaviour
    {
        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            var session = GameSession.Instance;
            if (session == null)
            {
                GUI.Label(new Rect(20, 20, 500, 20), "No GameSession — start play from the Bootstrap scene.");
                return;
            }

            var contracts = session.Contracts;
            var ps = session.PlayerState;

            GUILayout.BeginArea(new Rect(20, 20, 400, 620), GUI.skin.box);
            GUILayout.Label($"CITY      Gold: {ps.Currency}");
            GUILayout.Space(8);

            GUILayout.Label("Loadout (chosen from inventory):");
            DrawWeaponSelector("Left ", ps.SelectedLeft, ps.SetLeft, ps.OwnedWeapons);
            DrawWeaponSelector("Right", ps.SelectedRight, ps.SetRight, ps.OwnedWeapons);
            GUILayout.Space(10);

            if (contracts.Active == null)
            {
                GUILayout.Label("Contract Board:");
                ContractInstance toAccept = null;
                foreach (var c in contracts.Available)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{c.Title}  ({c.Purpose}, +{c.RewardCurrency}g)");
                    if (GUILayout.Button("Accept", GUILayout.Width(70)))
                        toAccept = c;
                    GUILayout.EndHorizontal();
                }
                if (toAccept != null)
                    contracts.Accept(toAccept);
            }
            else
            {
                var c = contracts.Active;
                GUILayout.Label($"Active: {c.Title}   {c.Progress}/{c.TargetCount}");
                GUILayout.Label(c.Description);
                if (c.IsComplete)
                {
                    if (GUILayout.Button($"Turn In  (+{c.RewardCurrency}g)", GUILayout.Height(28)))
                        ps.Currency += contracts.TurnIn();
                }
                else if (GUILayout.Button("Abandon"))
                {
                    contracts.Abandon();
                }
            }

            GUILayout.Space(12);
            if (GUILayout.Button("Depart to Frontier", GUILayout.Height(34)))
                GameScenes.LoadFrontier();

            GUILayout.EndArea();

            // Discovery journal.
            GUILayout.BeginArea(new Rect(440, 20, 360, 620), GUI.skin.box);
            GUILayout.Label($"DISCOVERIES ({session.Discovery.DiscoveredCount})");
            GUILayout.Space(4);
            bool any = false;
            foreach (var discovery in session.Discovery.DiscoveredCandidates())
            {
                GUILayout.Label($"• {discovery.Title}");
                any = true;
            }
            if (!any)
                GUILayout.Label("None yet — fight and explore to discover.");
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
    }
}
