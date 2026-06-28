using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/player")]
public class PlayerController : ControllerBase
{
    private readonly IPlayerDefinitionRepository _repo;

    public PlayerController(IPlayerDefinitionRepository repo) => _repo = repo;

    /// <summary>The player's balance stats (read-only). The client fetches these and
    /// applies them to movement / health / focus, so a balance edit retunes the player
    /// with no client rebuild.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var p = await _repo.GetAsync(ct);
        if (p is null) return NotFound();
        return Ok(new PlayerDefinitionResponse(
            p.MaxHealth, p.MoveSpeed, p.JumpVelocity, p.Gravity,
            p.DodgeSpeed, p.DodgeDuration, p.MaxFocus, p.FocusRegenPerSecond));
    }
}
