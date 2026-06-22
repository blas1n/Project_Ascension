using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _service;
    public ItemsController(IItemService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetByActor([FromQuery] Guid actorId, CancellationToken ct)
    {
        var result = await _service.GetByActorAsync(actorId, ct);
        return Ok(result.Value);
    }
}
