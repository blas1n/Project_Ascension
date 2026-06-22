using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/discoveries")]
public class DiscoveriesController : ControllerBase
{
    private readonly IDiscoveryService _service;
    public DiscoveriesController(IDiscoveryService service) => _service = service;

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
}
