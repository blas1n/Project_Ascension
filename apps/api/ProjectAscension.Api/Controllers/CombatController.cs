using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Responses;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/combat")]
public class CombatController : ControllerBase
{
    private readonly ICombatTuningProvider _tuning;

    public CombatController(ICombatTuningProvider tuning) => _tuning = tuning;

    /// <summary>The current combat balance values (read-only) — for inspection and
    /// verifying runtime edits to the combat-tuning row. In the slice the client fetches
    /// these and runs the resolvers; in the MMO the server runs them server-authoritatively.</summary>
    [HttpGet("tuning")]
    public async Task<IActionResult> GetTuning(CancellationToken ct)
    {
        var t = await _tuning.GetAsync(ct);
        return Ok(new CombatTuningResponse(
            t.ProjectileDamage, t.BeamDamage, t.AreaDamage, t.DotDamagePerTick, t.SpreadFalloff,
            t.BaseDotTicks, t.ShieldPerMagnitude, t.DashPerMagnitude, t.LeechFractionPerMagnitude,
            t.ControlDurationPerMagnitude, t.PassiveShieldReduction, t.PassiveBarrierReduction,
            t.PassiveLeech, t.FocusCostPerPoint));
    }
}
