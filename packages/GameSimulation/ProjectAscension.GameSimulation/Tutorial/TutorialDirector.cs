using System;

namespace ProjectAscension.GameSimulation.Tutorial
{
    /// <summary>
    /// The authored first-hour steps (docs/03-gameplay/first-hour-experience.md). The player learns the
    /// world's rules by DOING, so each step completes on a real gameplay signal — never on a "next"
    /// button. City selection is stubbed for the vertical slice (one city), keeping the sequence intact.
    /// </summary>
    public enum TutorialStep
    {
        CreateCharacter,        // 0 — name + appearance
        ChooseEquipment,        // 2 — pick two, BEFORE training (ADR 0016) — you can't be taught to
                                // swing a weapon you never chose
        Training,               // 3 — move, jump, evade a telegraph, attack — now with your own loadout
        FirstDiscovery,         // 4 — arises from behaviour, never granted
        AcceptSurveyContract,   // 5 — "외곽 조사" from the board
        EarnMap,                // 6 — reach the marker; the map is an ITEM
        AcceptDeepContract,     // 7 — "심층 조사" — secretly beyond you
        FirstDeath,             // 8 — the world is not safe
        DelegateContract,       // 9 — 위임: you need not do everything alone
        IssueContract,          // 10 — 발주: hire someone instead
        Return,                 // 11 — come home
        Complete,
    }

    /// <summary>
    /// What the player has actually DONE. A flags set rather than a single "current event" so signals
    /// can be BANKED: acting ahead of the script (discovering something during training, say) is
    /// remembered and auto-satisfies that step when the sequence reaches it — the tutorial can never
    /// soft-lock by demanding the player repeat something they already did.
    /// </summary>
    [Flags]
    public enum TutorialSignal
    {
        None = 0,
        NameChosen = 1 << 0,
        Moved = 1 << 1,
        Jumped = 1 << 2,
        Evaded = 1 << 3, // read a monster's wind-up and stepped out of range, making it whiff (ADR 0012)
        Attacked = 1 << 4,
        EquipmentChosen = 1 << 5,
        DiscoveryMade = 1 << 6,
        SurveyContractAccepted = 1 << 7,
        MapReceived = 1 << 8,
        DeepContractAccepted = 1 << 9,
        Died = 1 << 10,
        ContractDelegated = 1 << 11,
        ContractIssued = 1 << 12,
        ReturnedToCity = 1 << 13,
    }

    /// <summary>The player's place in the first hour: the current step, and everything they have done.</summary>
    public sealed record TutorialProgress(
        TutorialStep Step = TutorialStep.CreateCharacter,
        TutorialSignal Seen = TutorialSignal.None)
    {
        public static readonly TutorialProgress Start = new();
        public bool IsComplete => Step == TutorialStep.Complete;
    }

    /// <summary>
    /// The first-hour sequencer — pure and deterministic (ADR: Unity is a shell), so the whole authored
    /// experience is headless-testable instead of living in MonoBehaviour glue. The shell feeds it real
    /// gameplay signals (the player jumped, a discovery fired, a contract was delegated) and renders the
    /// current step's prompt; it makes no decisions.
    /// </summary>
    public static class TutorialDirector
    {
        /// <summary>Metres of real travel before the "you have moved" signal reads as true — UX, not
        /// economy, so (per CLAUDE.md) it does not need to be DB-driven; it just needs to not be a
        /// magic number sitting in the shell (ADR: Unity is a shell). The shell measures distance
        /// travelled; whether that's "enough" is this sequencer's call, same as every other step.</summary>
        public const float TravelToCountAsMovedMeters = 4f;

        /// <summary>Whether accumulated ground travel is enough to count as the Moved training signal.</summary>
        public static bool HasTravelledEnoughToCountAsMoved(float metersTravelled)
            => metersTravelled >= TravelToCountAsMovedMeters;

        /// <summary>What a step needs before the player has genuinely lived it.</summary>
        public static TutorialSignal Requirement(TutorialStep step) => step switch
        {
            TutorialStep.CreateCharacter => TutorialSignal.NameChosen,
            TutorialStep.ChooseEquipment => TutorialSignal.EquipmentChosen,
            TutorialStep.Training => TutorialSignal.Moved | TutorialSignal.Jumped
                                   | TutorialSignal.Evaded | TutorialSignal.Attacked,
            TutorialStep.FirstDiscovery => TutorialSignal.DiscoveryMade,
            TutorialStep.AcceptSurveyContract => TutorialSignal.SurveyContractAccepted,
            TutorialStep.EarnMap => TutorialSignal.MapReceived,
            TutorialStep.AcceptDeepContract => TutorialSignal.DeepContractAccepted,
            TutorialStep.FirstDeath => TutorialSignal.Died,
            TutorialStep.DelegateContract => TutorialSignal.ContractDelegated,
            TutorialStep.IssueContract => TutorialSignal.ContractIssued,
            TutorialStep.Return => TutorialSignal.ReturnedToCity,
            _ => TutorialSignal.None, // Complete — terminal
        };

        /// <summary>Record what the player did, then advance as far as the record allows. Advancing in a
        /// loop is what makes banking work: a step whose signal already arrived passes straight through.</summary>
        public static TutorialProgress Observe(TutorialProgress progress, TutorialSignal signal)
        {
            var seen = progress.Seen | signal;
            var step = progress.Step;

            while (step != TutorialStep.Complete && Satisfied(step, seen))
                step++;

            return new TutorialProgress(step, seen);
        }

        /// <summary>Whether the step's requirement is fully met by what the player has done.</summary>
        public static bool Satisfied(TutorialStep step, TutorialSignal seen)
        {
            var need = Requirement(step);
            return (seen & need) == need;
        }

        /// <summary>The training verbs still outstanding — the shell prompts for exactly what's left,
        /// so the player is never told to do something they've already done.</summary>
        public static TutorialSignal RemainingTraining(TutorialProgress progress)
            => Requirement(TutorialStep.Training) & ~progress.Seen;
    }
}
