using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>
    /// Deterministic discovery evaluation. On each behavior event, every candidate
    /// whose context is satisfied AND whose primary behavior matches AND whose
    /// prerequisite is met advances by one. The candidate matching the player's
    /// dominant behavior reaches its threshold first — so behavior, not a static
    /// recipe, decides which discovery emerges.
    /// </summary>
    public static class DiscoveryEngine
    {
        public static List<DiscoveryCandidate> Apply(DiscoveryState state, IReadOnlyList<DiscoveryCandidate> catalog,
            in Observation observation)
        {
            var unlocked = new List<DiscoveryCandidate>();
            for (int i = 0; i < catalog.Count; i++)
            {
                var candidate = catalog[i];
                if (state.Discovered.Contains(candidate.Key)) continue;
                if (candidate.Behavior != observation.Kind) continue;
                if (candidate.Prerequisite.Length > 0 && !state.Discovered.Contains(candidate.Prerequisite)) continue;
                if (!ContextSatisfied(candidate.RequiredContext, observation.Context)) continue;

                int progress = state.ProgressOf(candidate.Key) + 1;
                state.Progress[candidate.Key] = progress;
                if (progress >= candidate.RequiredProgress)
                {
                    state.Discovered.Add(candidate.Key);
                    unlocked.Add(candidate);
                }
            }
            return unlocked;
        }

        private static bool ContextSatisfied(IReadOnlyList<string> required, ISet<string> active)
        {
            for (int i = 0; i < required.Count; i++)
                if (!active.Contains(required[i])) return false;
            return true;
        }
    }
}
