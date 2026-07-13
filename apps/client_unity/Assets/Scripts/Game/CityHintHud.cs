using UnityEngine;
using ProjectAscension.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// A one-line orientation hint shown whenever no city station panel is open — the same role the
    /// old CityHub's idle message played, now that there are several stations instead of one. Reuses
    /// <see cref="UiFocus.IsFocused"/> as "is anything open right now" rather than tracking its own
    /// flag: every station panel already pushes/pops that gate, so it's a reliable signal for free.
    /// </summary>
    public sealed class CityHintHud : MonoBehaviour
    {
        private void OnGUI()
        {
            if (UiFocus.IsFocused) return;

            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
            GUI.Label(new Rect((Screen.width - 520f) * 0.5f, Screen.height - 92f, 520f, 20f),
                "The board, the armoury, and the city's people all have work for you.", style);
        }
    }
}
