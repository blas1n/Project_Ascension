using ProjectAscension.Domain.Enums;

namespace ProjectAscension.GameSimulation.Contracts
{
    /// <summary>
    /// Maps a server contract's fields into a runtime <see cref="ContractInstance"/> (ADR: Unity is a
    /// shell). Pure and primitive-in (the Unity code extracts the DTO fields and calls this), so the
    /// mapping decisions — purpose parse with a safe default, a target-count floor, an issuer
    /// default — are headless contract-tested instead of hiding in a MonoBehaviour.
    /// </summary>
    public static class ContractMapping
    {
        public static ContractInstance FromFields(
            string? purpose, string? title, string? description, int targetCount,
            int rewardCurrency, string? target, string? issuer, bool delegationAllowed,
            int rewardReputation, int minReputation, int timeLimitSeconds,
            bool failOnTimeout, bool failOnDeath,
            string? rewardItemKey = null, int rewardItemAmount = 0,
            // The server's contract row id — needed to accept/turn-in/delegate THIS contract
            // server-side later (ADR 0014). Unparseable/absent → Guid.Empty (no server backing).
            string? id = null)
        {
            System.Guid.TryParse(id, out var contractId);
            return new ContractInstance
            {
                Id = contractId,
                RewardItemKey = rewardItemKey ?? "",
                RewardItemAmount = rewardItemAmount < 0 ? 0 : rewardItemAmount,
                Purpose = System.Enum.TryParse<ContractPurpose>(purpose, out var p) ? p : ContractPurpose.Hunt,
                Title = title ?? "",
                Description = description ?? "",
                TargetCount = targetCount < 1 ? 1 : targetCount, // a contract always needs ≥1 to complete
                RewardCurrency = rewardCurrency,
                Target = target,
                Issuer = issuer ?? "",
                DelegationAllowed = delegationAllowed,
                RewardReputation = rewardReputation,
                MinReputation = minReputation,
                TimeLimitSeconds = timeLimitSeconds,
                FailOnTimeout = failOnTimeout,
                FailOnDeath = failOnDeath,
            };
        }
    }
}
