using System;
using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>
    /// Accumulates the behavior counts and context tags observed since the last flush,
    /// so the client can report a behavior signature to the server's discovery trigger
    /// (POST /api/discoveries/evaluate). Pure and deterministic — the Unity reporter is
    /// just IO around it.
    /// </summary>
    public sealed class BehaviorAccumulator
    {
        private readonly Dictionary<string, int> _counts = new();
        private readonly SortedSet<string> _tags = new(StringComparer.Ordinal);

        public bool HasActivity => _counts.Count > 0;
        public IReadOnlyDictionary<string, int> Counts => _counts;
        public IReadOnlyCollection<string> Tags => _tags;

        public void Record(BehaviorKind behavior) => Record(behavior.ToString());

        /// <summary>Record a behavior by key. Most are <see cref="BehaviorKind"/> names, but a FUSION
        /// (ADR 0008) is a pair — "Synthesis:arcane&gt;firearm" — and the pair is the whole point, so it
        /// cannot be an enum member.</summary>
        public void Record(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _counts[key] = (_counts.TryGetValue(key, out var c) ? c : 0) + 1;
        }

        /// <summary>Replaces the current situational context (equipment / environment).
        /// Tags persist across flushes — they describe where the behavior happens.</summary>
        public void SetContext(IEnumerable<string> tags)
        {
            _tags.Clear();
            foreach (var t in tags)
                if (!string.IsNullOrWhiteSpace(t))
                    _tags.Add(t);
        }

        /// <summary>Clears the accumulated behavior counts after a flush. Context tags
        /// are kept (the player is still in the same situation).</summary>
        public void Reset() => _counts.Clear();
    }
}
