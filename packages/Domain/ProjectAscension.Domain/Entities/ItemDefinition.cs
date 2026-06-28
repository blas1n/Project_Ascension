#nullable enable

namespace ProjectAscension.Domain.Entities
{
    /// <summary>
    /// A tradeable item's shop definition — display name and the city shop's buy/sell
    /// prices. Runtime-editable balance data; the slice's resource economy (monster drops
    /// → sell for gold; buy materials for settlement supply) reads these. A price of 0
    /// means that side of the trade is disabled.
    /// </summary>
    public class ItemDefinition
    {
        public string Key { get; set; } = string.Empty; // "hide","feather","core",...
        public string DisplayName { get; set; } = string.Empty;
        public int SellPrice { get; set; } // gold the shop pays the player (0 = not sellable)
        public int BuyPrice { get; set; }  // gold the player pays the shop (0 = not buyable)
    }
}
