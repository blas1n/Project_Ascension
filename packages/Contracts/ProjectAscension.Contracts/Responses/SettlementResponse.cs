#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>The frontier outpost's development — its name, civilization stage, and the
    /// maturity level (0–4) of each infrastructure track. The player grows these by
    /// delivering resources.</summary>
    public record SettlementResponse(
        string Name,
        string Stage,
        int ShelterLevel,
        int MarketLevel,
        int DefenseLevel,
        int TotalLevel);
}
