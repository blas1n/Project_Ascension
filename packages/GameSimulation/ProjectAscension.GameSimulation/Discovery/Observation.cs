using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>
    /// An observed gameplay fact the discovery engine evaluates. <see cref="Kind"/>
    /// is the behavior; <see cref="Context"/> is the situational tags (equipment now;
    /// environment/target later). Growing the condition surface adds fields here —
    /// not new parameters across the observe → engine chain.
    /// </summary>
    public readonly struct Observation
    {
        public Observation(BehaviorKind kind, ISet<string> context)
        {
            Kind = kind;
            Context = context;
        }

        public BehaviorKind Kind { get; }
        public ISet<string> Context { get; }
    }
}
