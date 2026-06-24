using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Game
{
    /// <summary>
    /// Runtime contract the player can accept and complete. Mirrors the persistent
    /// Domain.Contract but with typed objective fields (no JSON) for the slice.
    /// </summary>
    public sealed class ContractInstance
    {
        public ContractPurpose Purpose;
        public string Title = "";
        public string Description = "";
        public int TargetCount = 1;
        public int Progress;
        public int RewardCurrency;

        public bool IsComplete => Progress >= TargetCount;

        public ContractInstance Fresh() => new()
        {
            Purpose = Purpose,
            Title = Title,
            Description = Description,
            TargetCount = TargetCount,
            Progress = 0,
            RewardCurrency = RewardCurrency,
        };
    }
}
