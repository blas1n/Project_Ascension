using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/loadouts")]
public class LoadoutsController : ControllerBase
{
    private readonly ILoadoutService _service;
    public LoadoutsController(ILoadoutService service) => _service = service;

    [HttpGet("{actorId:guid}")]
    public async Task<IActionResult> Get(Guid actorId, CancellationToken ct)
    {
        var result = await _service.GetAsync(actorId, ct);
        return Ok(result.Value);
    }

    [HttpPut("{actorId:guid}")]
    public async Task<IActionResult> Update(Guid actorId, [FromBody] UpdateLoadoutRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(actorId, request, ct);
        return Ok(result.Value);
    }
}
