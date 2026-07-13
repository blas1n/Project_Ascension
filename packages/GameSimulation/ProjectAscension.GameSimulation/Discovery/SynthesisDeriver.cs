namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>
    /// Watches the two hands and notices when the player FUSES them (ADR 0008).
    ///
    /// This is the signal the discovery engine was missing. Before it, "Flame Bullet" came out of
    /// *carrying* a catalyst and a pistol and shooting a lot — the system read POSSESSION as synthesis,
    /// because possession was the only thing it could see. But the idea was always the act: you wreathe
    /// the shot and you fire it, near enough to the same moment that the two become one thing.
    /// So we observe the act instead of inferring it.
    ///
    /// ORDER is part of the signal, and that is the whole point: wreathing the shot
    /// (<c>arcane&gt;firearm</c>) and detonating what the shot left behind (<c>firearm&gt;arcane</c>) are
    /// different acts, and must be different discoveries. Same two hands, different play, different
    /// skill — the promise, made mechanical.
    ///
    /// Pure and headless-tested (ADR: Unity is a shell): the shell reports "this weapon was used, at
    /// this time"; the rule decides whether that was a fusion.
    /// </summary>
    public sealed class SynthesisDeriver
    {
        /// <summary>Behaviour keys carry this prefix so the scorer (and the AI) can tell a FUSION from
        /// a mere count. Shared with the server's TriggerEvaluator.</summary>
        public const string Prefix = "Synthesis:";

        /// <summary>"Almost the same moment." Long enough to be a human act, short enough that it can't
        /// be an accident of two unrelated attacks.</summary>
        public const float DefaultWindow = 0.5f;

        private readonly float _window;

        private string _lastTag;
        private float _lastTime = float.NegativeInfinity;

        public SynthesisDeriver(float window = DefaultWindow) => _window = window;

        /// <summary>
        /// Report that a weapon of this context ("arcane", "firearm", "melee", "bow"…) was just used.
        /// Returns the synthesis behaviour key if this use FUSED with the previous one — otherwise null.
        /// </summary>
        public string Used(string contextTag, float time)
        {
            if (string.IsNullOrEmpty(contextTag))
            {
                // An unclassifiable weapon can't fuse, and mustn't poison the next fusion either.
                _lastTag = null;
                _lastTime = time;
                return null;
            }

            string previous = _lastTag;
            float since = time - _lastTime;

            _lastTag = contextTag;
            _lastTime = time;

            // Fusing needs two DIFFERENT hands: firing the same kind twice is just firing twice.
            if (string.IsNullOrEmpty(previous) || previous == contextTag) return null;
            if (since > _window) return null; // too far apart to be one act

            return Prefix + previous + ">" + contextTag; // primer > delivery
        }

        /// <summary>Forget the last use — e.g. the player swapped loadout, or died. Two uses either side
        /// of that are not one act.</summary>
        public void Reset()
        {
            _lastTag = null;
            _lastTime = float.NegativeInfinity;
        }
    }
}
