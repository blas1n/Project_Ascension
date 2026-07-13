using System;
using System.Collections.Generic;

namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>
    /// The behavior composition grammar (ADR 0009). One act stream in, composite behaviours out.
    ///
    /// This replaces five bespoke observers — dodge-attack, air-attack, repeated-jump, charged-attack,
    /// weapon-fusion — each of which was its own hand-written special case. That was the same mistake
    /// ADR 0007 was written to undo: if every new idea needs a new engine feature, the discovery space
    /// is capped at the number of features we thought to write. So the engine owns the OPERATORS, and
    /// the combinations are whatever the player actually does.
    ///
    /// Four operators over the stream:
    ///   Fuse:a&gt;b    — b almost the same instant as a. The tight window. ("wreathe the shot, fire it")
    ///   Seq:a&gt;b     — b right after a. The loose window.  ("roll, then shoot")
    ///   While:a@q   — a done while some quality held.      ("shoot while airborne")
    ///   Chain:a     — a done again and again.              ("keep jumping")
    ///
    /// Fuse and Seq are the same shape at different tightness, and that gap IS the signal: weaving two
    /// hands in a tenth of a second is a different mastery from stringing them over half a second, and
    /// must be able to become a different discovery.
    ///
    /// Pure and headless-tested (ADR: Unity is a shell) — the shell reports acts; the grammar decides
    /// what they add up to. Nothing here knows what a catalyst or a pistol is.
    /// </summary>
    public sealed class CompositionDeriver
    {
        // Scored by PREFIX (server-side), so a brand-new act or weapon opens brand-new combinations
        // without seeding a single row.
        public const string FusePrefix = "Fuse:";
        public const string SeqPrefix = "Seq:";
        public const string WhilePrefix = "While:";
        public const string ChainPrefix = "Chain:";

        /// <summary>"Almost the same instant" — deliberate, hard to do by accident.</summary>
        public const float DefaultFuseWindow = 0.22f;
        /// <summary>"Right after" — one act flowing into the next.</summary>
        public const float DefaultSeqWindow = 0.65f;
        /// <summary>Repeats this close keep a chain alive.</summary>
        public const float DefaultChainWindow = 1.2f;
        /// <summary>A chain this long stops being an accident.</summary>
        public const int DefaultChainLength = 3;

        private readonly float _fuseWindow;
        private readonly float _seqWindow;
        private readonly float _chainWindow;
        private readonly int _chainLength;

        private string _lastToken;
        private float _lastTime;

        private string _chainToken;
        private int _chainCount;
        private float _chainTime;

        public CompositionDeriver(
            float fuseWindow = DefaultFuseWindow,
            float seqWindow = DefaultSeqWindow,
            float chainWindow = DefaultChainWindow,
            int chainLength = DefaultChainLength)
        {
            _fuseWindow = fuseWindow;
            _seqWindow = seqWindow;
            _chainWindow = chainWindow;
            _chainLength = chainLength;
        }

        /// <summary>
        /// Observe one act and append every composite behaviour it completes. An act can complete more
        /// than one at once — a third quick jump that is also airborne is both a chain and a quality.
        /// </summary>
        public void Observe(Act act, ICollection<string> into)
        {
            if (!act.IsValid || into == null) return;

            string token = act.Token;
            float t = act.Time;

            // While: — the act carried a quality. "Shot it while falling" is one act, not two events.
            foreach (var quality in Qualities(act.Qualities))
                into.Add(WhilePrefix + token + "@" + quality);

            // Fuse: / Seq: — composition with the act before it. Two DIFFERENT things; doing the same
            // thing twice is a chain, not a combination.
            if (!string.IsNullOrEmpty(_lastToken) && _lastToken != token)
            {
                float gap = t - _lastTime;
                if (gap >= 0f && gap <= _fuseWindow) into.Add(FusePrefix + _lastToken + ">" + token);
                else if (gap >= 0f && gap <= _seqWindow) into.Add(SeqPrefix + _lastToken + ">" + token);
            }

            // Chain: — the same thing, again, and again. Emits once the chain is long enough, and for
            // every repeat after that: a player still bouncing is still doing the thing.
            if (_chainToken == token && t - _chainTime <= _chainWindow) _chainCount++;
            else { _chainToken = token; _chainCount = 1; }
            _chainTime = t;
            if (_chainCount >= _chainLength) into.Add(ChainPrefix + token);

            _lastToken = token;
            _lastTime = t;
        }

        /// <summary>Forget the stream — death, a loadout swap, leaving the region. Acts either side of
        /// that are not one act.</summary>
        public void Reset()
        {
            _lastToken = null;
            _lastTime = 0f;
            _chainToken = null;
            _chainCount = 0;
            _chainTime = 0f;
        }

        private static IEnumerable<string> Qualities(ActQuality q)
        {
            if ((q & ActQuality.Airborne) != 0) yield return "airborne";
            if ((q & ActQuality.Charged) != 0) yield return "charged";
            if ((q & ActQuality.Blocking) != 0) yield return "blocking";
            if ((q & ActQuality.Aiming) != 0) yield return "aiming";
            if ((q & ActQuality.Moving) != 0) yield return "moving";
            if ((q & ActQuality.Dodging) != 0) yield return "dodging";
        }
    }
}
