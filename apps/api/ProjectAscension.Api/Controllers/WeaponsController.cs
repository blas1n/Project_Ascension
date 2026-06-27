using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/weapons")]
public class WeaponsController : ControllerBase
{
    private readonly IWeaponDefinitionRepository _repo;

    public WeaponsController(IWeaponDefinitionRepository repo) => _repo = repo;

    /// <summary>The authored weapon definitions (read-only). The client fetches these
    /// and builds its weapon instances, so a balance edit to a row retunes the weapon
    /// (e.g. arrow drop) with no client rebuild.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var weapons = await _repo.GetAllAsync(ct);
        return Ok(weapons.Select(w => new WeaponDefinitionResponse(
            w.Key, w.DisplayName, w.EquipmentType, w.SlotType,
            w.Damage, w.Range, w.ProjectileSpeed, w.ProjectileGravity, w.Cooldown,
            w.ChargeTime, w.MaxChargeMultiplier,
            w.SpreadMin, w.SpreadMax, w.SpreadPerShot, w.SpreadRecovery)));
    }
}
