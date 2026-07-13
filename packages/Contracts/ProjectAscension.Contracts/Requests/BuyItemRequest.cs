#nullable enable

namespace ProjectAscension.Contracts.Requests
{
    /// <summary>Buy an item from the city shop. Only the INTENT (what, how many) — the server
    /// prices it from its own item catalog (ADR 0014).</summary>
    public record BuyItemRequest(string ItemKey, int Quantity);
}
