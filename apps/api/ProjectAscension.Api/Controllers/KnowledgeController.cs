using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Extensions;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

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

    /// <summary>Sell a license for an owned discovery's knowledge — once per discovery
    /// (server-enforced). Price/reputation are derived from the skill's own composed effect
    /// graph and DB-driven tuning, never the request body.</summary>
    [HttpPost("license")]
    public async Task<IActionResult> License([FromBody] LicenseKnowledgeRequest request, CancellationToken ct)
        => (await _service.LicenseAsync(request, ct)).ToActionResult(this);
}
