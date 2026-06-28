using System.Collections.Generic;
using ProjectAscension.Combat;
using ProjectAscension.Domain.Enums;

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
            if (contract != null) Available.Add(contract);
        }

        public void Accept(ContractInstance template) => Active = template.Fresh();

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
            Active.Progress = System.Math.Min(Active.TargetCount, Active.Progress + amount);
        }

        /// <summary>Hand in a completed contract; returns the reward (0 if not completable).</summary>
        public int TurnIn()
        {
            if (Active == null || !Active.IsComplete) return 0;
            int reward = Active.RewardCurrency;
            Active = null;
            return reward;
        }
    }
}
