using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Recognizes a command's invocation combo from the live button-input stream. A
    /// discovered Command is invoked by performing the button sequence the rule engine
    /// assigned it (not by re-performing the discovery behavior, and not a single
    /// button) — so double jump and dodge-slash are invoked the same way. Feed inputs
    /// as they happen; when a registered sequence appears as the tail within the time
    /// window, its command fires. Deterministic — the caller supplies the time.
    /// </summary>
    public sealed class ComboRecognizer
    {
        public const float DefaultWindow = 1.5f;

        private readonly float _window;
        private readonly List<(InputToken Token, float Time)> _recent = new();
        private readonly List<Registration> _commands = new();

        public ComboRecognizer(float window = DefaultWindow) => _window = window;

        private readonly struct Registration
        {
            public readonly IReadOnlyList<InputToken> Sequence;
            public readonly DiscoveredSkill Skill;
            public Registration(IReadOnlyList<InputToken> sequence, DiscoveredSkill skill)
            {
                Sequence = sequence;
                Skill = skill;
            }
        }

        /// <summary>Register a command's combo. Ignored (returns false) if the combo has
        /// fewer than two inputs — there is nothing to recognize.</summary>
        public bool Register(IReadOnlyList<InputToken> combo, DiscoveredSkill skill)
        {
            if (combo == null || combo.Count < 2) return false;
            _commands.Add(new Registration(combo, skill));
            return true;
        }

        /// <summary>Feed one button input at the given time; returns the command whose
        /// combo just completed (its inputs are then consumed), or null.</summary>
        public DiscoveredSkill Feed(InputToken token, float time)
        {
            _recent.Add((token, time));
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

        private bool Matches(IReadOnlyList<InputToken> seq, float now)
        {
            if (_recent.Count < seq.Count) return false;
            int offset = _recent.Count - seq.Count;
            for (int i = 0; i < seq.Count; i++)
                if (_recent[offset + i].Token != seq[i]) return false;
            return now - _recent[offset].Time <= _window;
        }
    }
}
