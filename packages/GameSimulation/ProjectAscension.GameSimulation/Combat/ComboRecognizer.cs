using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Recognizes a command's invocation combo from the live button-input stream. A registered
    /// sequence fires the instant it appears as the tail of the current input chain.
    ///
    /// Timing is PER-GAP, not total-span: each input only has to land within the window of the
    /// PREVIOUS one, so a longer combo is no harder to time than a short one (a gap bigger than
    /// the window breaks the chain and starts fresh). The longest tail match wins, and combos
    /// are assigned PREFIX-FREE (server-side, ComboAssigner), so no combo shadows a longer one —
    /// there's nothing to wait for, and the match fires immediately. Deterministic — the caller
    /// supplies the time.
    /// </summary>
    public sealed class ComboRecognizer
    {
        public const float DefaultWindow = 1.5f; // max gap between consecutive inputs

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

        /// <summary>Feed one button input at the given time; returns the command whose combo is
        /// the (longest) tail of the current chain, or null.</summary>
        public DiscoveredSkill Feed(InputToken token, float time)
        {
            // A gap longer than the window breaks the chain — start fresh. This is the whole
            // timing rule: only the gap to the PREVIOUS input matters.
            if (_recent.Count > 0 && time - _recent[_recent.Count - 1].Time > _window)
                _recent.Clear();
            _recent.Add((token, time));

            var match = LongestTailMatch();
            if (match == null) return null;
            _recent.Clear();
            return match.Value.Skill;
        }

        private Registration? LongestTailMatch()
        {
            Registration? best = null;
            foreach (var reg in _commands)
                if (MatchesTail(reg.Sequence) && (best == null || reg.Sequence.Count > best.Value.Sequence.Count))
                    best = reg;
            return best;
        }

        private bool MatchesTail(IReadOnlyList<InputToken> seq)
        {
            if (_recent.Count < seq.Count) return false;
            int offset = _recent.Count - seq.Count;
            for (int i = 0; i < seq.Count; i++)
                if (_recent[offset + i].Token != seq[i]) return false;
            return true;
        }
    }
}
