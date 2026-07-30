using ProjectAscension.GameSimulation.Tutorial;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Tutorial
{
    /// <summary>
    /// The authored first hour, headless (ADR: Unity is a shell) — so the whole onboarding sequence is
    /// verified without Unity. The properties that matter: every step completes only by DOING the thing,
    /// the sequence can't be skipped, and it can never soft-lock a player who acts ahead of the script.
    /// </summary>
    public class TutorialDirectorTests
    {
        private static TutorialProgress Observe(TutorialProgress p, params TutorialSignal[] signals)
        {
            foreach (var s in signals) p = TutorialDirector.Observe(p, s);
            return p;
        }

        [Fact]
        public void StartsAtCharacterCreation_AndAdvancesOnlyWhenTheStepIsLived()
        {
            var p = TutorialProgress.Start;
            Assert.Equal(TutorialStep.CreateCharacter, p.Step);

            // An unrelated signal does not advance the step.
            p = Observe(p, TutorialSignal.Jumped);
            Assert.Equal(TutorialStep.CreateCharacter, p.Step);

            // ADR 0016: equipment comes before training — you choose your loadout first.
            p = Observe(p, TutorialSignal.NameChosen);
            Assert.Equal(TutorialStep.ChooseEquipment, p.Step);
        }

        [Fact]
        public void Training_NeedsAllFourVerbs()
        {
            // ADR 0016: ChooseEquipment happens before Training.
            var p = Observe(TutorialProgress.Start, TutorialSignal.NameChosen, TutorialSignal.EquipmentChosen);
            Assert.Equal(TutorialStep.Training, p.Step);

            p = Observe(p, TutorialSignal.Moved, TutorialSignal.Jumped, TutorialSignal.Evaded);
            Assert.Equal(TutorialStep.Training, p.Step); // attack still outstanding
            Assert.Equal(TutorialSignal.Attacked, TutorialDirector.RemainingTraining(p));

            p = Observe(p, TutorialSignal.Attacked);
            Assert.Equal(TutorialStep.FirstDiscovery, p.Step);
            Assert.Equal(TutorialSignal.None, TutorialDirector.RemainingTraining(p));
        }

        [Fact]
        public void SignalsAreBanked_SoActingAheadOfTheScriptNeverSoftLocks()
        {
            // The player discovers something during training (discovery arises from behaviour — it does
            // not wait for the tutorial). Later, the FirstDiscovery step must pass straight through
            // rather than demand a second discovery.
            var p = Observe(TutorialProgress.Start,
                TutorialSignal.DiscoveryMade,      // ahead of the script
                TutorialSignal.NameChosen,
                TutorialSignal.EquipmentChosen,    // ADR 0016: equipment before training
                TutorialSignal.Moved, TutorialSignal.Jumped, TutorialSignal.Evaded, TutorialSignal.Attacked);

            // Training completed → FirstDiscovery was already banked → skipped to the next step.
            Assert.Equal(TutorialStep.AcceptSurveyContract, p.Step);
        }

        [Fact]
        public void CannotSkipAhead_ALaterSignalDoesNotAdvanceAnEarlierStep()
        {
            // Dying early does not push the player past the steps they haven't lived.
            var p = Observe(TutorialProgress.Start, TutorialSignal.Died, TutorialSignal.ContractIssued);
            Assert.Equal(TutorialStep.CreateCharacter, p.Step);
        }

        [Fact]
        public void TheWholeFirstHour_RunsToCompletionInOrder()
        {
            var p = Observe(TutorialProgress.Start,
                TutorialSignal.NameChosen,
                TutorialSignal.EquipmentChosen,
                TutorialSignal.Moved, TutorialSignal.Jumped, TutorialSignal.Evaded, TutorialSignal.Attacked,
                TutorialSignal.DiscoveryMade,
                TutorialSignal.SurveyContractAccepted,
                TutorialSignal.MapReceived,
                TutorialSignal.DeepContractAccepted,
                TutorialSignal.Died,
                TutorialSignal.ContractDelegated,
                TutorialSignal.ContractIssued,
                TutorialSignal.ReturnedToCity);

            Assert.Equal(TutorialStep.Complete, p.Step);
            Assert.True(p.IsComplete);
        }

        [Fact]
        public void Complete_IsTerminal_AndFurtherSignalsAreHarmless()
        {
            var done = new TutorialProgress(TutorialStep.Complete, TutorialSignal.None);
            var after = TutorialDirector.Observe(done, TutorialSignal.Died);

            Assert.Equal(TutorialStep.Complete, after.Step); // never loops past the end
            Assert.True(after.IsComplete);
        }

        [Fact]
        public void RepeatingASignal_IsIdempotent()
        {
            var p = Observe(TutorialProgress.Start, TutorialSignal.NameChosen, TutorialSignal.NameChosen);
            Assert.Equal(TutorialStep.ChooseEquipment, p.Step);
        }

        // "Has moved enough to count" is UX, not economy — not DB-driven per CLAUDE.md — but it must
        // still be the sequencer's call, not a constant sitting in the shell (ADR: Unity is a shell).

        [Fact]
        public void BelowTheTravelThreshold_HasNotMovedEnough()
        {
            Assert.False(TutorialDirector.HasTravelledEnoughToCountAsMoved(3.99f));
        }

        [Fact]
        public void AtOrAboveTheTravelThreshold_HasMovedEnough()
        {
            Assert.True(TutorialDirector.HasTravelledEnoughToCountAsMoved(TutorialDirector.TravelToCountAsMovedMeters));
            Assert.True(TutorialDirector.HasTravelledEnoughToCountAsMoved(10f));
        }
    }
}
