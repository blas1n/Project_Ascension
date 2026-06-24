using UnityEngine;

namespace ProjectAscension.Game
{
    /// <summary>Reaching this marker completes a Survey contract.</summary>
    public sealed class SurveyPoint : PlayerTriggerVolume
    {
        protected override void OnPlayerEntered()
        {
            if (GameSession.Instance != null)
                GameSession.Instance.Contracts.ReportSurvey();
        }
    }
}
