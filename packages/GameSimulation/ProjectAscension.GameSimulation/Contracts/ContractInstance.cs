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
        /// <summary>The server's contract row id (System.Guid.Empty for a local-only default —
        /// an offline board entry with no server backing, which therefore cannot be turned in
        /// server-side; ADR 0014 — the server, not the client, decides completion + payout).</summary>
        public System.Guid Id;

        public ContractPurpose Purpose;
        public string Title = "";
        public string Description = "";
        public int TargetCount = 1;
        public int Progress;
        public int RewardCurrency;
        public int RewardReputation; // 명성 gained on completion

        /// <summary>An ITEM handed over on completion — the first hour's survey pays in a map, not gold
        /// (docs/03-gameplay/first-hour-experience.md). Empty key = no item reward.</summary>
        public string RewardItemKey = "";
        public int RewardItemAmount;

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
            Id = Id,
            Purpose = Purpose,
            Title = Title,
            Description = Description,
            TargetCount = TargetCount,
            Progress = 0,
            RewardCurrency = RewardCurrency,
            RewardReputation = RewardReputation,
            RewardItemKey = RewardItemKey,
            RewardItemAmount = RewardItemAmount,
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
