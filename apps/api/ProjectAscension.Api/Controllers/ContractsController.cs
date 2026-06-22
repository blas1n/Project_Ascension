using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Extensions;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractService _service;
    public ContractsController(IContractService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetByRegion([FromQuery] Guid regionId, CancellationToken ct)
    {
        var result = await _service.GetByRegionAsync(regionId, ct);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AcceptContractRequest request, CancellationToken ct)
        => (await _service.AcceptAsync(id, request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        => (await _service.CompleteAsync(id, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateContractProgressRequest request, CancellationToken ct)
        => (await _service.UpdateProgressAsync(id, request, ct)).ToActionResult(this);
}
