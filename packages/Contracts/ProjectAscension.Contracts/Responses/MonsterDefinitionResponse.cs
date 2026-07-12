#nullable enable

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>A monster type's combat stats (read-only view) — the client builds its
    /// monsters from these, so balance edits retune them with no client rebuild.</summary>
    public record MonsterDefinitionResponse(
        string Key,
        float MaxHealth,
        float MoveSpeed,
        float AggroRange,
        float AttackRange,
        float AttackCooldown,
        float AttackWindup,
        float Damage,
        float ProjectileSpeed,
        float Scale,
        string DropItemKey,
        int DropAmount);
}
