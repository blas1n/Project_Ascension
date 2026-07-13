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

        // A caller destroyed while still holding focus (e.g. "Depart to Frontier" unloads the City
        // scene from inside the equipment station's own OnGUI) must still release it — an unmatched
        // Push leaves gameplay input disabled for the whole next scene.
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

        private void OnGUI()
        {
            if (!IsOpen) return;
            DrawPanel();
        }

        protected abstract void DrawPanel();
    }
}
