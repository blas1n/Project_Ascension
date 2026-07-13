using UnityEngine;
using UnityEngine.InputSystem;
using ProjectAscension.Player;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Shared plumbing every city station panel needs: open/close, taking (and releasing) the
    /// UiFocus gate while open, and Esc-to-close. Before this split each station re-implemented the
    /// same Push/Pop/Esc bookkeeping by hand (the old CityHub did it twice, for two panels sharing
    /// one pair of open flags) — now every station is a thin <see cref="DrawPanel"/> override and
    /// this is the only place that bookkeeping lives.
    ///
    /// A subclass decides HOW it opens — an <see cref="ProjectAscension.World.Interactable"/>'s
    /// Interacted event for a physical station (the board, the equipment rack, an NPC), a raw
    /// hotkey for the discovery journal — by calling <see cref="Toggle"/> itself; this class only
    /// owns what happens once <see cref="IsOpen"/> changes.
    /// </summary>
    public abstract class CityStationPanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        private bool _focusHeld;

        protected void Toggle()
        {
            IsOpen = !IsOpen;
            ApplyFocus();
            if (IsOpen) OnOpened();
        }

        protected void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            ApplyFocus();
        }

        /// <summary>Called each time the panel transitions closed to open — override to reset
        /// per-visit state (e.g. clear a stale quote).</summary>
        protected virtual void OnOpened() { }

        private void ApplyFocus()
        {
            if (IsOpen && !_focusHeld) { UiFocus.Push(); _focusHeld = true; }
            else if (!IsOpen && _focusHeld) { UiFocus.Pop(); _focusHeld = false; }
        }

        // A caller destroyed while still holding focus must still release it — an unmatched Push
        // leaves gameplay input disabled for the whole next scene. (Historical case: the old
        // "Depart to Frontier" button lived inside the equipment station's own OnGUI and unloaded
        // the City scene from within it, panel still open — DepartZone/CityBlockout's gate pad
        // replaced that button so departure never happens from inside an open panel any more, but
        // this defensive Pop stays as the general safety net for any panel destroyed while open.)
        protected virtual void OnDestroy()
        {
            if (_focusHeld) { UiFocus.Pop(); _focusHeld = false; }
        }

        // Esc is the universal "back out" for every station. Subclasses that also want a hotkey to
        // close (the discovery journal's J) override this and call base.Update() first so Esc
        // always works regardless.
        protected virtual void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        /// <summary>
        /// The top-left corner of a centred block of this size. A station panel is a MODAL — you are
        /// standing at the board, doing one thing — so it belongs in the middle of the screen, where
        /// you are already looking.
        ///
        /// These panels inherited (20, 20) from the old CityHub, which put them straight on top of the
        /// contract tracker and the rest of the top-left HUD. Two things drawn over each other are not
        /// two things: they are one unreadable thing.
        /// </summary>
        protected static Vector2 ModalOrigin(float width, float height)
            => new Vector2(Mathf.Round((Screen.width - width) * 0.5f),
                           Mathf.Round((Screen.height - height) * 0.5f));

        private void OnGUI()
        {
            if (!IsOpen) return;

            // Behind the panel, the world waits — and the HUD underneath stops competing with it.
            var was = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = was;

            DrawPanel();
        }

        protected abstract void DrawPanel();
    }
}
