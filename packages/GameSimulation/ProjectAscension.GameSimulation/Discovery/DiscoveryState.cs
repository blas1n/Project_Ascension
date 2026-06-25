using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>Per-actor discovery progress and the set of unlocked discoveries.</summary>
    public sealed class DiscoveryState
    {
        public Dictionary<string, int> Progress { get; } = new();
        public HashSet<string> Discovered { get; } = new();

        public int ProgressOf(string key) => Progress.TryGetValue(key, out var value) ? value : 0;
        public bool IsDiscovered(string key) => Discovered.Contains(key);
    }
}
