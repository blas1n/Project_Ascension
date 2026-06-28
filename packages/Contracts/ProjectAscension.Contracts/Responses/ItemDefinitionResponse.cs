#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>A shop item's prices (read-only) — the client builds its city shop from
    /// these, so balance edits retune the economy with no client rebuild.</summary>
    public record ItemDefinitionResponse(string Key, string DisplayName, int SellPrice, int BuyPrice);
}
