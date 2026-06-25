using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/discoveries")]
public class DiscoveriesController : ControllerBase
{
    private readonly IDiscoveryService _service;
    private readonly ISkillCompositionService _composition;

    public DiscoveriesController(IDiscoveryService service, ISkillCompositionService composition)
    {
        _service = service;
        _composition = composition;
    }

    [HttpGet]
    public async Task<IActionResult> GetByActor([FromQuery] Guid actorId, CancellationToken ct)
    {
        var result = await _service.GetByActorAsync(actorId, ct);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordDiscoveryRequest request, CancellationToken ct)
    {
        var result = await _service.RecordAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByActor), new { actorId = request.ActorId }, result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>Fixes the discovery fact instantly and queues AI content (Pending).
    /// Returns the discovery id to poll for the composed skill.</summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger([FromBody] TriggerDiscoveryRequest request, CancellationToken ct)
    {
        var discoveryId = await _composition.TriggerAsync(request, ct);
        return AcceptedAtAction(nameof(GetSkill), new { discoveryId }, new { discoveryId, status = "Pending" });
    }

    /// <summary>Polls a discovery's content: Pending until the AI composes it, then
    /// the frozen skill.</summary>
    [HttpGet("{discoveryId:guid}/skill")]
    public async Task<IActionResult> GetSkill(Guid discoveryId, CancellationToken ct)
    {
        var skill = await _composition.GetByDiscoveryAsync(discoveryId, ct);
        return skill is null ? NotFound() : Ok(skill);
    }
}
