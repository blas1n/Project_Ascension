using ProjectAscension.Domain.Enums;

namespace ProjectAscension.GameSimulation.Contracts
{
    /// <summary>
    /// Runtime contract the player can accept and complete (ADR: Unity is a shell — domain data
    /// lives in GameSimulation, not Assets). Mirrors the persistent Domain.Contract but with typed
    /// objective fields (no JSON) for the slice. Its rules live in <see cref="ContractRules"/>.
    /// </summary>
    public sealed class ContractInstance
    {
        public ContractPurpose Purpose;
        public string Title = "";
        public string Description = "";
        public int TargetCount = 1;
        public int Progress;
        public int RewardCurrency;
        public int RewardReputation; // 명성 gained on completion
        public int MinReputation;    // standing required to accept (0 = open to all)
        public int TimeLimitSeconds; // deadline once accepted (0 = no limit)
        public float Remaining;      // runtime countdown after accepting
        public bool FailOnTimeout;   // specified failure conditions — absent = never fails
        public bool FailOnDeath;
        public string Issuer = "";   // the NPC/office that issued it (empty for player-issued)

        /// <summary>Optional objective filter — for a hunt, the monster key ("elite") that
        /// counts; null/empty means any target satisfies the objective.</summary>
        public string Target;

        /// <summary>Whether the holder may delegate (re-issue) this contract instead of
        /// clearing it themselves — the delegation tutorial uses this.</summary>
        public bool DelegationAllowed;

        public bool IsComplete => ContractRules.IsComplete(Progress, TargetCount);

        public ContractInstance Fresh() => new()
        {
            Purpose = Purpose,
            Title = Title,
            Description = Description,
            TargetCount = TargetCount,
            Progress = 0,
            RewardCurrency = RewardCurrency,
            RewardReputation = RewardReputation,
            MinReputation = MinReputation,
            TimeLimitSeconds = TimeLimitSeconds,
            Remaining = TimeLimitSeconds,
            FailOnTimeout = FailOnTimeout,
            FailOnDeath = FailOnDeath,
            Target = Target,
            DelegationAllowed = DelegationAllowed,
        };
    }
}
