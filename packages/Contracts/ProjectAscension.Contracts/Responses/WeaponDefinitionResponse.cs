#nullable enable
using ProjectAscension.Domain.Enums;

namespace ProjectAscension.Contracts.Responses
{
    /// <summary>An authored weapon's stats (read-only view) — the client builds its
    /// weapon instances from these, so balance edits to the row retune the weapon with
    /// no client rebuild.</summary>
    public record WeaponDefinitionResponse(
        string Key,
        string DisplayName,
        EquipmentType EquipmentType,
        SlotType SlotType,
        float Damage,
        float Range,
        float ProjectileSpeed,
        float ProjectileGravity,
        float Cooldown,
        float ChargeTime,
        float MaxChargeMultiplier,
        float SpreadMin,
        float SpreadMax,
        float SpreadPerShot,
        float SpreadRecovery,
        int MagazineSize,
        float ReloadTime);
}
