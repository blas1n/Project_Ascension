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
    /// prematurely re-enabling gameplay while the other is still up.
    ///
    /// This is the ONE OWNER of the cursor. It does not save and restore what it found, it DICTATES:
    /// a panel is up, so the cursor is free; no panel is up, so the cursor belongs to the camera.
    /// Saving-and-restoring was the bug — the character-creation form is already open when the city
    /// scene loads, and PlayerController.Start() then locked the cursor out from under it, so the
    /// name field could not be clicked and the player could not type their own name. Anyone who wants
    /// the cursor asks this class, and this class answers with whether a panel is open.
    ///
    /// A caller that might be destroyed while still holding focus (e.g. a scene unload mid-panel)
    /// MUST Pop in OnDestroy — see CityStationPanel's defensive Pop, which exists because the old
    /// "Depart to Frontier" button used to end the scene from inside its own open equipment station
    /// panel. Departure is a world pad now (DepartZone), never a button inside a panel, but the
    /// defensive Pop remains the general safety net. An unmatched Push leaves gameplay input
    /// permanently disabled.
    /// </summary>
    public static class UiFocus
    {
        private static int _count;

        public static bool IsFocused => _count > 0;

        /// <summary>Put the cursor where the current focus says it belongs. Anything that thinks it
        /// wants to grab the cursor (PlayerController on spawn, a scene load) calls THIS instead of
        /// setting Cursor itself — so it cannot steal the cursor from an open panel.</summary>
        public static void ApplyCursor()
        {
            Cursor.lockState = IsFocused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = IsFocused;
        }

        /// <summary>Raised when focus transitions 0→1 (true) or 1→0 (false) — PlayerInputHandler
        /// disables/enables the "Player" action map on this; anything polling raw input directly
        /// (AbilitySlots) should just check <see cref="IsFocused"/>.</summary>
        public static event Action<bool> Changed;

        public static void Push()
        {
            _count++;
            if (_count == 1)
            {
                ApplyCursor();
                Changed?.Invoke(true);
            }
        }

        public static void Pop()
        {
            if (_count == 0) return; // an unmatched Pop is a caller bug, not a reason to go negative
            _count--;
            if (_count == 0)
            {
                ApplyCursor();
                Changed?.Invoke(false);
            }
        }
    }
}
