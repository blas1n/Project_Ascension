using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/knowledge")]
public class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeService _service;
    public KnowledgeController(IKnowledgeService service) => _service = service;

    /// <summary>Lists the knowledge assets owned by an actor (the discoveries they
    /// own — the discoverer is the first owner).</summary>
    [HttpGet]
    public async Task<IActionResult> GetByOwner([FromQuery] Guid ownerActorId, CancellationToken ct)
    {
        var result = await _service.GetByOwnerAsync(ownerActorId, ct);
        return Ok(result.Value);
    }
}
