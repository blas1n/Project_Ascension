using UnityEngine;

namespace ProjectAscension.Player
{
    /// <summary>
    /// The "[F] Contract Board" prompt. Drawn only while <see cref="InteractionSensor.Current"/> is
    /// non-null, so it can never claim something is interactable when the sensor disagrees — this HUD
    /// makes no decision of its own, it only renders the sensor's pick. Styled after
    /// TutorialRunner's prompt (bold, centred, white) but anchored lower-middle so the two never
    /// collide when both are on screen at once.
    /// </summary>
    public sealed class InteractPromptHud : MonoBehaviour
    {
        private void OnGUI()
        {
            var target = InteractionSensor.Current;
            if (target == null) return;

            const float w = 420f, h = 26f;
            var rect = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.74f, w, h);

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
            };
            style.normal.textColor = Color.white;

            // The key name comes from the actual binding (PlayerInputHandler.InteractKeyLabel), not a
            // hardcoded "F" — a rebind updates this prompt with it.
            GUI.Label(rect, $"[{PlayerInputHandler.InteractKeyLabel}] {target.Label}", style);
        }
    }
}
