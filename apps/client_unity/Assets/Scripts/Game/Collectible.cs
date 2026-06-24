using UnityEngine;

namespace ProjectAscension.Game
{
    /// <summary>A pickup that advances a Collection contract when the player touches it.</summary>
    public sealed class Collectible : PlayerTriggerVolume
    {
        protected override void OnPlayerEntered()
        {
            if (GameSession.Instance != null)
                GameSession.Instance.Contracts.ReportCollect();
            Destroy(gameObject);
        }
    }
}
