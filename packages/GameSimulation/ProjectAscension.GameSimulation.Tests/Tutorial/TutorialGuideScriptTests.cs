using ProjectAscension.GameSimulation.Tutorial;
using Xunit;

namespace ProjectAscension.GameSimulation.Tests.Tutorial
{
    /// <summary>
    /// The guide's script, headless (ADR: Unity is a shell). Properties that matter: every LIVED step
    /// gives the player a line, only a step with an actual PLACE gets a marker (see
    /// TutorialGuideStation.None's own doc comment for why CreateCharacter/FirstDiscovery/FirstDeath
    /// are deliberately station-less), and Complete — the guide's exit — has neither.
    /// </summary>
    public class TutorialGuideScriptTests
    {
        private static readonly TutorialStep[] AllStepsExceptComplete =
        {
            TutorialStep.CreateCharacter, TutorialStep.Training, TutorialStep.ChooseEquipment,
            TutorialStep.FirstDiscovery, TutorialStep.AcceptSurveyContract, TutorialStep.EarnMap,
            TutorialStep.AcceptDeepContract, TutorialStep.FirstDeath, TutorialStep.DelegateContract,
            TutorialStep.IssueContract, TutorialStep.Return,
        };

        [Fact]
        public void EveryStepButComplete_HasALine()
        {
            foreach (var step in AllStepsExceptComplete)
                Assert.False(string.IsNullOrWhiteSpace(TutorialGuideScript.For(step).Text), $"{step} has no line.");
        }

        [Fact]
        public void Complete_HasNeitherLineNorStation()
        {
            var line = TutorialGuideScript.For(TutorialStep.Complete);
            Assert.True(string.IsNullOrEmpty(line.Text));
            Assert.Equal(TutorialGuideStation.None, line.Station);
        }

        // The persistent objective tracker (client HUD) reads Objective, distinct from the guide's
        // spoken Text — every LIVED step except CreateCharacter (the character sheet already owns
        // that moment) needs one, or the tracker goes blank mid-tutorial.
        private static readonly TutorialStep[] StepsWithATrackedObjective =
        {
            TutorialStep.Training, TutorialStep.ChooseEquipment, TutorialStep.FirstDiscovery,
            TutorialStep.AcceptSurveyContract, TutorialStep.EarnMap, TutorialStep.AcceptDeepContract,
            TutorialStep.FirstDeath, TutorialStep.DelegateContract, TutorialStep.IssueContract,
            TutorialStep.Return,
        };

        [Fact]
        public void EveryStepWithATrackedObjective_HasOne()
        {
            foreach (var step in StepsWithATrackedObjective)
                Assert.False(string.IsNullOrWhiteSpace(TutorialGuideScript.For(step).Objective), $"{step} has no objective.");
        }

        [Theory]
        [InlineData(TutorialStep.CreateCharacter)] // the character sheet already owns this moment
        [InlineData(TutorialStep.Complete)]        // no more tutorial
        public void StepsWithNoTrackedObjective_HaveNone(TutorialStep step)
        {
            Assert.True(string.IsNullOrEmpty(TutorialGuideScript.For(step).Objective));
        }

        [Fact]
        public void Objective_IsNeverTheSameStringAsTheSpokenLine()
        {
            // The guide SPEAKS in character; the tracker STATES the task. If they ever collide
            // verbatim, either the objective is unauthored copy-paste or the line lost its voice.
            foreach (var step in StepsWithATrackedObjective)
            {
                var line = TutorialGuideScript.For(step);
                Assert.NotEqual(line.Text, line.Objective);
            }
        }

        [Theory]
        [InlineData(TutorialStep.Training, TutorialGuideStation.TrainingGround)]           // 2단계 훈련장
        [InlineData(TutorialStep.ChooseEquipment, TutorialGuideStation.EquipmentStation)]  // 3단계 첫 장비 선택
        [InlineData(TutorialStep.AcceptSurveyContract, TutorialGuideStation.ContractBoard)]// 5단계 게시판
        [InlineData(TutorialStep.EarnMap, TutorialGuideStation.SurveyMarker)]              // 6단계 외곽 목표 지점
        [InlineData(TutorialStep.AcceptDeepContract, TutorialGuideStation.ContractBoard)]  // 7단계 게시판
        [InlineData(TutorialStep.DelegateContract, TutorialGuideStation.Clerk)]            // 9단계 위임
        [InlineData(TutorialStep.IssueContract, TutorialGuideStation.Clerk)]               // 10단계 발주
        [InlineData(TutorialStep.Return, TutorialGuideStation.ReturnPad)]                  // 11단계 첫 귀환
        public void TargetStation_MatchesTheDocsPlaceForThatStage(TutorialStep step, TutorialGuideStation expected)
        {
            Assert.Equal(expected, TutorialGuideScript.For(step).Station);
        }

        [Theory]
        [InlineData(TutorialStep.CreateCharacter)] // 0단계 — a screen, not a place
        [InlineData(TutorialStep.FirstDiscovery)]  // 4단계 — arises from behaviour, not a destination
        [InlineData(TutorialStep.FirstDeath)]      // 8단계 — a directed ambush; marking it would spoil it
        public void StepsWithNoPlace_HaveNoStation(TutorialStep step)
        {
            Assert.Equal(TutorialGuideStation.None, TutorialGuideScript.For(step).Station);
        }
    }
}
