using UnityEngine;
using ProjectAscension.Combat;

namespace ProjectAscension.Game
{
    /// <summary>A sample pickup. Announces the collection fact; it doesn't know who
    /// listens (contracts, discovery, …).</summary>
    public sealed class Collectible : PlayerTriggerVolume
    {
        protected override void OnPlayerEntered()
        {
            GameplayEvents.RaiseSampleCollected(gameObject);
            Destroy(gameObject);
        }
    }
}
