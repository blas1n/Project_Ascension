using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>
    /// A possible discovery. The equipment/knowledge context defines the
    /// possibility space; the behavior selects which discovery emerges. Critically,
    /// several candidates can share the same context but differ by behavior — so the
    /// same combination yields different discoveries depending on how the player acts.
    /// </summary>
    public sealed class DiscoveryCandidate
    {
        public string Key { get; }
        public string Title { get; }
        public string Description { get; }
        public BehaviorKind Behavior { get; }

        /// <summary>Context tags (equipment/knowledge) that must all be active.</summary>
        public IReadOnlyList<string> RequiredContext { get; }
        public int RequiredProgress { get; }

        /// <summary>A discovery that must be unlocked first ("" = none). Forms the discovery graph.</summary>
        public string Prerequisite { get; }

        public DiscoveryCandidate(string key, string title, string description, BehaviorKind behavior,
            string[] requiredContext, int requiredProgress, string prerequisite = "")
        {
            Key = key;
            Title = title;
            Description = description;
            Behavior = behavior;
            RequiredContext = requiredContext;
            RequiredProgress = requiredProgress;
            Prerequisite = prerequisite;
        }
    }
}
