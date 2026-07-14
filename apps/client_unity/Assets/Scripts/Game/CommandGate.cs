using System.Collections.Generic;
using ProjectAscension.Equipment;
using ProjectAscension.GameSimulation.Combat;

namespace ProjectAscension.Game
{
    /// <summary>
    /// ADR 0011 — a command is bound to the weapons that actually MADE it, and only those. The rule
    /// itself is the sim's (SkillBinding, headless-tested); this only reports what is in the player's
    /// hands right now, so swapping weapons enables or disables a command.
    ///
    /// The old gate demanded every tag the player happened to be CARRYING at discovery — so holding a
    /// catalyst you never used would hold a gun technique hostage. Now the evidence is what you DID.
    /// </summary>
    public static class CommandGate
    {
        // BoundInstruments, not RequiredEquipment: a command discovered through a forged weapon
        // (its own "spell:" tag) is gated on that weapon too, not just the four base categories —
        // see SkillBinding.BoundInstruments. This is what the label shown next to an ability slot
        // must agree with, or the UI would say "no requirement" for a command the gate still blocks.
        public static IReadOnlyCollection<string> RequiredEquipment(DiscoveredSkill command)
            => SkillBinding.BoundInstruments(command?.Behaviors);

        public static bool Invocable(DiscoveredSkill command, ICollection<string> currentTags)
            => SkillBinding.Usable(command?.Behaviors, currentTags);
    }
}
