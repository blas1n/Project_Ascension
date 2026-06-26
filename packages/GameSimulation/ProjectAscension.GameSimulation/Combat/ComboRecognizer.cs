using System.Collections.Generic;
using ProjectAscension.GameSimulation.Discovery;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Recognizes a command's invocation combo from the live behavior stream. A
    /// discovered Command is invoked by re-performing the behavior sequence that
    /// characterized its discovery (dodge → attack), not a dedicated button (ADR/
    /// discovery.md — the system reacts to behavior). Feed behaviors as they happen;
    /// when a registered sequence appears as the tail within the time window, its
    /// command fires. Deterministic — the caller supplies the time.
    /// </summary>
    public sealed class ComboRecognizer
    {
        public const float DefaultWindow = 1.5f;

        private readonly float _window;
        private readonly List<(BehaviorKind Kind, float Time)> _recent = new();
        private readonly List<Registration> _commands = new();

        public ComboRecognizer(float window = DefaultWindow) => _window = window;

        private readonly struct Registration
        {
            public readonly IReadOnlyList<BehaviorKind> Sequence;
            public readonly DiscoveredSkill Skill;
            public Registration(IReadOnlyList<BehaviorKind> sequence, DiscoveredSkill skill)
            {
                Sequence = sequence;
                Skill = skill;
            }
        }

        /// <summary>Register a command's combo. Ignored (returns false) if the combo has
        /// fewer than two behaviors — there is nothing to recognize.</summary>
        public bool Register(IReadOnlyList<BehaviorKind> combo, DiscoveredSkill skill)
        {
            if (combo == null || combo.Count < 2) return false;
            _commands.Add(new Registration(combo, skill));
            return true;
        }

        /// <summary>Feed one behavior at the given time; returns the command whose combo
        /// just completed (its inputs are then consumed), or null.</summary>
        public DiscoveredSkill Feed(BehaviorKind kind, float time)
        {
            _recent.Add((kind, time));
            _recent.RemoveAll(e => time - e.Time > _window);

            foreach (var reg in _commands)
            {
                if (Matches(reg.Sequence, time))
                {
                    _recent.Clear();
                    return reg.Skill;
                }
            }
            return null;
        }

        private bool Matches(IReadOnlyList<BehaviorKind> seq, float now)
        {
            if (_recent.Count < seq.Count) return false;
            int offset = _recent.Count - seq.Count;
            for (int i = 0; i < seq.Count; i++)
                if (_recent[offset + i].Kind != seq[i]) return false;
            return now - _recent[offset].Time <= _window;
        }
    }
}
