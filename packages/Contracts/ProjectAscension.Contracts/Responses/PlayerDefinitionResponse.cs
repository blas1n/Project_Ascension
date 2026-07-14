#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>The player's balance stats (read-only view) — the client applies these to
    /// movement and health, so balance edits retune the player with no rebuild.</summary>
    public record PlayerDefinitionResponse(
        float MaxHealth,
        float MoveSpeed,
        float JumpVelocity,
        float Gravity);
}
