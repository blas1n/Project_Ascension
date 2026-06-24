using UnityEngine;

namespace ProjectAscension.Game
{
    /// <summary>Frontier HUD: shows the active contract objective and progress (display only).</summary>
    public sealed class ContractHud : MonoBehaviour
    {
        private void OnGUI()
        {
            var session = GameSession.Instance;
            if (session == null) return;

            GUI.Box(new Rect(10, 10, 340, 100), "Contract");
            var c = session.Contracts.Active;
            if (c == null)
            {
                GUI.Label(new Rect(20, 35, 320, 20), "No active contract.");
            }
            else
            {
                GUI.Label(new Rect(20, 35, 320, 20), c.Title);
                GUI.Label(new Rect(20, 55, 320, 20), c.Description);
                var status = c.IsComplete ? "  COMPLETE - return to City" : "";
                GUI.Label(new Rect(20, 75, 320, 20), $"Progress: {c.Progress}/{c.TargetCount}{status}");
            }

            GUI.Label(new Rect(20, 115, 420, 20), $"Gold: {session.PlayerState.Currency}    (green pad = return to City)");
        }
    }
}
