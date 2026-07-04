using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Combat
{
    /// <summary>
    /// Recognizes a command's invocation combo from the live button-input stream. A
    /// discovered Command is invoked by performing the button sequence the rule engine
    /// assigned it. Feed inputs as they happen; a registered sequence fires when it appears
    /// as the tail of the current input chain.
    ///
    /// Timing is PER-GAP, not total-span: each input only has to land within the window of
    /// the PREVIOUS one, so a longer combo isn't harder to time than a short one (a gap
    /// bigger than the window breaks the chain and starts fresh). When a completed combo is
    /// also the PREFIX of a longer registered combo, it is DEFERRED for a short disambiguation
    /// window (poll <see cref="Tick"/>) so the longer combo isn't permanently shadowed — e.g.
    /// "Dodge,Jump" doesn't steal every "Dodge,Jump,RMB". Deterministic — the caller supplies
    /// the time.
    /// </summary>
    public sealed class ComboRecognizer
    {
        public const float DefaultWindow = 1.5f;          // max gap between consecutive inputs
        public const float DefaultDisambiguation = 0.4f;  // grace to extend a prefix into a longer combo

        private readonly float _window;
        private readonly float _disambiguation;
        private readonly List<(InputToken Token, float Time)> _recent = new();
        private readonly List<Registration> _commands = new();

        private DiscoveredSkill _pending;   // a completed prefix, awaiting a possible extension
        private float _pendingDeadline;

        public ComboRecognizer(float window = DefaultWindow, float disambiguation = DefaultDisambiguation)
        {
            _window = window;
            _disambiguation = disambiguation;
        }

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

        /// <summary>Feed one button input at the given time; returns the command to invoke
        /// now, or null (no match yet, or the match was deferred — poll <see cref="Tick"/>).</summary>
        public DiscoveredSkill Feed(InputToken token, float time)
        {
            // A gap longer than the window breaks the chain — start a fresh sequence. This is
            // the whole timing rule: only the gap to the PREVIOUS input matters.
            if (_recent.Count > 0 && time - _recent[_recent.Count - 1].Time > _window)
                _recent.Clear();
            _recent.Add((token, time));

            // The longest registered combo that is the tail of the current chain.
            var best = LongestTailMatch();
            if (best == null) return null; // no match; keep the chain (and any pending) alive

            var match = best.Value;
            // If a longer registered combo starts with this match, the player may still be
            // mid-combo — defer, so the shorter one doesn't permanently shadow the longer.
            if (HasLongerStartingWith(match.Sequence))
            {
                _pending = match.Skill;
                _pendingDeadline = time + _disambiguation;
                return null;
            }

            return Fire(match.Skill);
        }

        /// <summary>Fire a deferred (prefix) command once its extension window has lapsed —
        /// call each frame. Returns the command to invoke, or null.</summary>
        public DiscoveredSkill Tick(float time)
            => _pending != null && time >= _pendingDeadline ? Fire(_pending) : null;

        private DiscoveredSkill Fire(DiscoveredSkill skill)
        {
            _pending = null;
            _recent.Clear();
            return skill;
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

        private bool HasLongerStartingWith(IReadOnlyList<InputToken> prefix)
        {
            foreach (var reg in _commands)
                if (reg.Sequence.Count > prefix.Count && StartsWith(reg.Sequence, prefix))
                    return true;
            return false;
        }

        private static bool StartsWith(IReadOnlyList<InputToken> sequence, IReadOnlyList<InputToken> prefix)
        {
            if (sequence.Count < prefix.Count) return false;
            for (int i = 0; i < prefix.Count; i++)
                if (sequence[i] != prefix[i]) return false;
            return true;
        }
    }
}
