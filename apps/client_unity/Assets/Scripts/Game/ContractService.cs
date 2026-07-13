using System.Collections.Generic;
using ProjectAscension.Combat;
using ProjectAscension.Domain.Enums;
using ProjectAscension.GameSimulation.Contracts;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Tracks available contracts, the active one, and its progress. Lives for the
    /// whole session (owned by GameSession) so progress survives City&lt;-&gt;Frontier.
    /// A pure observer of <see cref="GameplayEvents"/> facts — objectives and
    /// monsters announce what happened; this maps the relevant facts to progress.
    /// </summary>
    public sealed class ContractService
    {
        public List<ContractInstance> Available { get; } = new();
        public ContractInstance Active { get; private set; }

        /// <summary>The contract beats the first-hour director listens for (docs/03-gameplay/
        /// first-hour-experience.md): taking one from the board, handing it off (위임), issuing
        /// one (발주). Facts, not decisions — the director interprets them.</summary>
        public event System.Action<ContractInstance> Accepted;
        public event System.Action<ContractInstance> HandedOff;
        public event System.Action<ContractInstance> Issued;

        /// <summary>Replace the board with DB-driven contracts (fetched at startup). A null
        /// or empty list is ignored, so an offline session keeps the built-in defaults.</summary>
        public void SetAvailable(List<ContractInstance> contracts)
        {
            if (contracts == null || contracts.Count == 0) return;
            Available.Clear();
            Available.AddRange(contracts);
        }

        public ContractService()
        {
            Available.Add(new ContractInstance
            {
                Purpose = ContractPurpose.Hunt, Title = "Cull the Beasts",
                Description = "Defeat 5 monsters in the frontier.", TargetCount = 5, RewardCurrency = 120,
            });
            Available.Add(new ContractInstance
            {
                Purpose = ContractPurpose.Survey, Title = "Map the Frontier",
                Description = "Reach the survey marker.", TargetCount = 1, RewardCurrency = 80,
            });
            Available.Add(new ContractInstance
            {
                Purpose = ContractPurpose.Collection, Title = "Gather Samples",
                Description = "Collect 3 samples.", TargetCount = 3, RewardCurrency = 90,
            });

            GameplayEvents.MonsterKilled += OnMonsterKilled;
            GameplayEvents.SampleCollected += OnSampleCollected;
            GameplayEvents.MarkerSurveyed += OnMarkerSurveyed;
        }

        /// <summary>Add a player-issued contract to the board (already calibrated by the server).</summary>
        public void AddIssued(ContractInstance contract)
        {
            if (contract == null) return;
            Available.Add(contract);
            Issued?.Invoke(contract);
        }

        // Delegation (위임): contracts the player handed to a contractor instead of clearing
        // themselves, each finishing after a delay (the stub NPC contractor). Completed
        // titles surface as a one-time message the city reads — the tutorial's payoff.
        public sealed class Delegated { public ContractInstance Contract; public float Remaining; }
        public List<Delegated> InProgress { get; } = new();
        public List<string> ContractorCompleted { get; } = new();

        /// <summary>Hand the active contract to a contractor (only if it allows delegation).
        /// The caller escrows the reward. Returns true if delegated.</summary>
        public bool DelegateActive(float seconds)
        {
            if (Active == null || !Active.DelegationAllowed) return false;
            var handed = Active;
            InProgress.Add(new Delegated { Contract = handed, Remaining = seconds });
            Active = null;
            HandedOff?.Invoke(handed);
            return true;
        }

        /// <summary>Advance the stub contractor — completes delegated contracts whose timer
        /// elapsed (works across scenes while the player plays). Driven by GameSession.</summary>
        public void TickDelegations(float dt)
        {
            for (int i = InProgress.Count - 1; i >= 0; i--)
            {
                var (remaining, elapsed) = ContractRules.TickTimer(InProgress[i].Remaining, dt);
                InProgress[i].Remaining = remaining;
                if (elapsed)
                {
                    ContractorCompleted.Add(InProgress[i].Contract.Title);
                    InProgress.RemoveAt(i);
                }
            }
        }

        /// <summary>The payout of a completed contract — currency, standing (명성), and possibly an ITEM
        /// (the first hour's survey pays in a map). An empty ItemKey means no item was owed.</summary>
        public readonly struct Reward
        {
            public readonly int Currency;
            public readonly int Reputation;
            public readonly string ItemKey;
            public readonly int ItemAmount;

            public Reward(int currency, int reputation, string itemKey = "", int itemAmount = 0)
            {
                Currency = currency;
                Reputation = reputation;
                ItemKey = itemKey ?? "";
                ItemAmount = itemAmount;
            }
        }

        /// <summary>Whether the player's standing meets the contract's requirement.</summary>
        public static bool CanAccept(ContractInstance c, int reputation) => c != null && ContractRules.CanAccept(reputation, c.MinReputation);

        public void Accept(ContractInstance template)
        {
            Active = template.Fresh();
            Accepted?.Invoke(Active);
        }

        public void Abandon() => Active = null;

        // A targeted hunt only counts kills of its target monster type (the objective
        // filter); an untargeted hunt counts any kill.
        private void OnMonsterKilled(UnityEngine.GameObject monster)
        {
            if (Active == null || Active.Purpose != ContractPurpose.Hunt) return;
            if (!string.IsNullOrEmpty(Active.Target))
            {
                var info = monster != null ? monster.GetComponent<IMonsterInfo>() : null;
                if (info == null || info.DiscoveryTag != "monster:" + Active.Target) return;
            }
            Advance(ContractPurpose.Hunt, 1);
        }
        private void OnSampleCollected(UnityEngine.GameObject _) => Advance(ContractPurpose.Collection, 1);
        private void OnMarkerSurveyed(UnityEngine.GameObject _) =>
            Advance(ContractPurpose.Survey, Active != null ? Active.TargetCount : 0);

        private void Advance(ContractPurpose purpose, int amount)
        {
            if (Active == null || Active.Purpose != purpose || Active.IsComplete) return;
            Active.Progress = ContractRules.ClampedProgress(Active.Progress, amount, Active.TargetCount);
        }

        /// <summary>Hand in a completed contract; returns the reward (0 if not completable).</summary>
        public Reward TurnIn()
        {
            if (Active == null || !Active.IsComplete) return default;
            var reward = new Reward(Active.RewardCurrency, Active.RewardReputation,
                Active.RewardItemKey, Active.RewardItemAmount);
            Active = null;
            return reward;
        }

        // Failure: a contract whose specified failure condition triggered. Surfaced for the
        // city. Failure is opt-in — only contracts with a failOn condition can fail.
        public List<string> FailedRecently { get; } = new();

        /// <summary>Advance the active contract's deadline. Returns the contract if it
        /// fails on timeout and just expired (the caller applies the penalty), else null.</summary>
        public ContractInstance TickActive(float dt)
        {
            if (Active == null || !ContractRules.CanExpire(Active.FailOnTimeout, Active.IsComplete)) return null;
            var (remaining, elapsed) = ContractRules.TickTimer(Active.Remaining, dt);
            Active.Remaining = remaining;
            if (!elapsed) return null;
            var failed = Active;
            Active = null;
            return failed;
        }

        /// <summary>The active contract fails if it specifies death as a failure condition.
        /// Returns the failed contract (caller applies the penalty), else null — so a death
        /// during a non-death-fail contract (e.g. the delegation tutorial) does NOT fail it.</summary>
        public ContractInstance FailActiveOnDeath()
        {
            if (Active == null || !ContractRules.FailsOnDeath(Active.FailOnDeath, Active.IsComplete)) return null;
            var failed = Active;
            Active = null;
            return failed;
        }
    }
}
