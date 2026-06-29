#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>A city/world NPC (read-only) — name and role. The MVP's static NPC presence
    /// (shop, guard, contract clerk).</summary>
    public record NpcResponse(string Name, string Role);
}
