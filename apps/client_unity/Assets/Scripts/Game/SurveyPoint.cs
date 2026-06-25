using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Game
{
    /// <summary>A survey marker. Announces the survey fact; it doesn't know who
    /// listens (contracts, discovery, …).</summary>
    public sealed class SurveyPoint : PlayerTriggerVolume
    {
        protected override void OnPlayerEntered()
        {
            GameplayEvents.RaiseMarkerSurveyed(gameObject);
        }
    }
}
