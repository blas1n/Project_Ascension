using System;

namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>Qualities that were TRUE OF an act while it happened (ADR 0009). Not separate events —
    /// a shot fired in the air is one act with a quality, not "an attack" plus "an air attack".</summary>
    [Flags]
    public enum ActQuality
    {
        None = 0,
        Airborne = 1 << 0, // off the ground
        Charged = 1 << 1,  // released after a real draw/hold
        Blocking = 1 << 2, // the shield was up
        Aiming = 1 << 3,   // reserved — the grammar will pick it up the day aiming exists
        Moving = 1 << 4,   // under way, not standing still
    }

    /// <summary>
    /// One thing the player did (ADR 0009): a verb, what they did it WITH, when, and what was true of
    /// them at the time. This is the ONLY input the discovery grammar needs — every composite
    /// (fusions, air attacks, chained jumps) is derived from a stream of these, rather
    /// than each getting its own bespoke observer.
    /// </summary>
    public readonly struct Act
    {
        /// <summary>What was done: "jump", "attack", "land"…</summary>
        public readonly string Verb;

        /// <summary>What it was done WITH — for an attack, the weapon's context ("arcane", "firearm",
        /// "bow", "melee"). Null when the verb needs no instrument (a jump is just a jump).</summary>
        public readonly string Instrument;

        public readonly float Time;
        public readonly ActQuality Qualities;

        public Act(string verb, string instrument, float time, ActQuality qualities = ActQuality.None)
        {
            Verb = verb;
            Instrument = instrument;
            Time = time;
            Qualities = qualities;
        }

        /// <summary>How this act names itself in a composition. An attack names its WEAPON, because
        /// rolling into a gunshot and rolling into a sword are not the same skill — keeping the
        /// instrument is what makes the grammar sharper than the special cases it replaces.</summary>
        public string Token => string.IsNullOrEmpty(Instrument) ? Verb : Instrument;

        public bool IsValid => !string.IsNullOrEmpty(Token);
    }
}
