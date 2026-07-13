using System;
using UnityEngine;

namespace ProjectAscension.Player
{
    /// <summary>
    /// Shared UI-focus gate (BUG 3: typing your name also drove the player). While any keyboard-
    /// focused modal panel is open — character creation's name field, the city's contract board /
    /// quartermaster panel, and whatever the next one turns out to be — gameplay input must not
    /// also react to the same keystrokes. Rather than each panel ad hoc toggling the cursor and
    /// hoping nothing else is listening, a panel calls <see cref="Push"/> when it opens and
    /// <see cref="Pop"/> when it closes; everything that reads player input honours
    /// <see cref="IsFocused"/> (or the <see cref="Changed"/> event) instead of polling ad hoc.
    ///
    /// A counter, not a bool: two panels can overlap (in principle) without one's close
    /// prematurely re-enabling gameplay while the other is still up. The cursor's lock state is
    /// captured on the FIRST Push and restored on the LAST Pop, so a panel never has to guess what
    /// "normal" looks like — it gets back exactly what was there before it opened.
    ///
    /// A caller that might be destroyed while still holding focus (e.g. a scene unload mid-panel)
    /// MUST Pop in OnDestroy — see CityStationPanel, whose equipment station panel is up for the
    /// "Depart to Frontier" button that ends the scene. An unmatched Push leaves gameplay input
    /// permanently disabled.
    /// </summary>
    public static class UiFocus
    {
        private static int _count;
        private static CursorLockMode _savedLockState;
        private static bool _savedVisible;

        public static bool IsFocused => _count > 0;

        /// <summary>Raised when focus transitions 0→1 (true) or 1→0 (false) — PlayerInputHandler
        /// disables/enables the "Player" action map on this; anything polling raw input directly
        /// (AbilitySlots) should just check <see cref="IsFocused"/>.</summary>
        public static event Action<bool> Changed;

        public static void Push()
        {
            if (_count == 0)
            {
                _savedLockState = Cursor.lockState;
                _savedVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Changed?.Invoke(true);
            }
            _count++;
        }

        public static void Pop()
        {
            if (_count == 0) return; // an unmatched Pop is a caller bug, not a reason to go negative
            _count--;
            if (_count == 0)
            {
                Cursor.lockState = _savedLockState;
                Cursor.visible = _savedVisible;
                Changed?.Invoke(false);
            }
        }
    }
}
