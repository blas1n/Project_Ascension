namespace ProjectAscension.GameSimulation.Discovery
{
    /// <summary>
    /// Derives the COMPOSITE behaviours — the ones a player performs but never explicitly "does":
    /// attacking out of a dodge, striking from the air, chaining jumps. These are the whole point of
    /// the discovery promise ("같은 지식이라도 플레이 방식에 따라 다른 발견이 생성된다"): the raw verbs
    /// are the same for everyone, so it is the SHAPE of play that has to differentiate a discovery.
    ///
    /// A rule, not MonoBehaviour glue (ADR: Unity is a shell) — the shell reports when a jump/dodge/
    /// attack happened and whether the player was airborne; this decides what that MEANS. Windows and
    /// thresholds live here so "what counts as a dodge-attack" is one tested answer, not a magic number
    /// buried in a reporter.
    /// </summary>
    public sealed class BehaviorDeriver
    {
        /// <summary>An attack landing this soon after a dodge reads as a dodge-attack (회피 직후 공격).</summary>
        public const float DefaultDodgeAttackWindow = 0.6f;
        /// <summary>Jumps chained within this of each other continue a streak.</summary>
        public const float DefaultJumpChainWindow = 1.2f;
        /// <summary>A streak this long reads as deliberate repeated jumping (반복 점프).</summary>
        public const int DefaultRepeatedJumpCount = 3;

        private readonly float _dodgeAttackWindow;
        private readonly float _jumpChainWindow;
        private readonly int _repeatedJumpCount;

        private float _lastDodgeTime = float.NegativeInfinity;
        private float _lastJumpTime = float.NegativeInfinity;
        private int _jumpStreak;

        public BehaviorDeriver(
            float dodgeAttackWindow = DefaultDodgeAttackWindow,
            float jumpChainWindow = DefaultJumpChainWindow,
            int repeatedJumpCount = DefaultRepeatedJumpCount)
        {
            _dodgeAttackWindow = dodgeAttackWindow;
            _jumpChainWindow = jumpChainWindow;
            _repeatedJumpCount = repeatedJumpCount;
        }

        /// <summary>The current chain length — exposed for tests/telemetry.</summary>
        public int JumpStreak => _jumpStreak;

        public void Dodged(float time) => _lastDodgeTime = time;

        /// <summary>A jump happened. Returns true once the chain is long enough to read as REPEATED
        /// jumping — and stays true for each further jump in the chain, because a player who keeps
        /// bouncing is still doing the thing.</summary>
        public bool Jumped(float time)
        {
            bool chains = time - _lastJumpTime <= _jumpChainWindow;
            _jumpStreak = chains ? _jumpStreak + 1 : 1;
            _lastJumpTime = time;
            return _jumpStreak >= _repeatedJumpCount;
        }

        /// <summary>Whether an attack at this moment came out of a dodge.</summary>
        public bool IsDodgeAttack(float time) => time - _lastDodgeTime <= _dodgeAttackWindow;
    }
}
