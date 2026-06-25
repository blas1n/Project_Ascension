using System;
using System.Collections.Generic;
using ProjectAscension.GameSimulation.Discovery;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Session-wide discovery progress. Wraps the deterministic DiscoveryEngine and
    /// raises Unlocked when a new discovery is made. Lives in GameSession so it
    /// persists across City &lt;-&gt; Frontier.
    /// </summary>
    public sealed class DiscoveryService
    {
        private readonly DiscoveryState _state = new();

        public event Action<DiscoveryCandidate> Unlocked;

        public void Observe(in Observation observation)
        {
            var unlocked = DiscoveryEngine.Apply(_state, DiscoveryCatalog.All, observation);
            for (int i = 0; i < unlocked.Count; i++)
                Unlocked?.Invoke(unlocked[i]);
        }

        public bool IsDiscovered(string key) => _state.IsDiscovered(key);
        public int ProgressOf(string key) => _state.ProgressOf(key);
        public int DiscoveredCount => _state.Discovered.Count;

        public IEnumerable<DiscoveryCandidate> DiscoveredCandidates()
        {
            foreach (var candidate in DiscoveryCatalog.All)
                if (_state.IsDiscovered(candidate.Key))
                    yield return candidate;
        }
    }
}
