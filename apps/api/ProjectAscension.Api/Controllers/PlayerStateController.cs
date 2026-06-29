using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Extensions;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/player-state")]
public class PlayerStateController : ControllerBase
{
    private readonly IPlayerProfileService _service;
    public PlayerStateController(IPlayerProfileService service) => _service = service;

    /// <summary>Load the saved player progress (currency, standing, materials, licensed knowledge).</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => (await _service.GetAsync(ct)).ToActionResult(this);

    /// <summary>Persist the player's current progress.</summary>
    [HttpPut]
    public async Task<IActionResult> Save([FromBody] SavePlayerStateRequest request, CancellationToken ct)
        => (await _service.SaveAsync(request, ct)).ToActionResult(this);
}
