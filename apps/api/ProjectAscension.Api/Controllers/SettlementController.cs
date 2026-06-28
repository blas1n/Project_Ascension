using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Extensions;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/settlement")]
public class SettlementController : ControllerBase
{
    private readonly ISettlementService _service;
    public SettlementController(ISettlementService service) => _service = service;

    /// <summary>The frontier outpost's current development (stage + infrastructure levels).</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => (await _service.GetAsync(ct)).ToActionResult(this);

    /// <summary>Deliver a resource to the outpost — matures the matching infrastructure and
    /// may advance the settlement's stage. Returns the new development state.</summary>
    [HttpPost("deliver")]
    public async Task<IActionResult> Deliver([FromBody] DeliverResourceRequest request, CancellationToken ct)
        => (await _service.DeliverAsync(request, ct)).ToActionResult(this);
}
