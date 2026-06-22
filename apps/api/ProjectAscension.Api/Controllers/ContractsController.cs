using Microsoft.AspNetCore.Mvc;
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
    {
        var result = await _service.AcceptAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : Conflict(result.Error);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _service.CompleteAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : Conflict(result.Error);
    }
}
