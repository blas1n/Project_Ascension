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

    /// <summary>The calibrated reward (suggested + band) for a prospective objective — the
    /// issuing UI reads this live so the player picks generosity, not balance math.</summary>
    [HttpGet("quote")]
    public async Task<IActionResult> GetQuote(
        [FromQuery] Domain.Enums.ContractPurpose purpose, [FromQuery] string? target, [FromQuery] int count, CancellationToken ct)
        => (await _service.GetQuoteAsync(purpose, target, count, ct)).ToActionResult(this);

    /// <summary>A player issues a contract: they choose the objective and generosity; the
    /// server calibrates/validates the reward and fills the copy, then opens it.</summary>
    [HttpPost]
    public async Task<IActionResult> Issue([FromBody] IssueContractRequest request, CancellationToken ct)
        => (await _service.IssueAsync(request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AcceptContractRequest request, CancellationToken ct)
        => (await _service.AcceptAsync(id, request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        => (await _service.CompleteAsync(id, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateContractProgressRequest request, CancellationToken ct)
        => (await _service.UpdateProgressAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Hand in a completed contract — the server pays the reward from its own stored
    /// terms and returns the resulting authoritative player state (ADR 0014).</summary>
    [HttpPost("{id:guid}/turn-in")]
    public async Task<IActionResult> TurnIn(Guid id, [FromBody] TurnInContractRequest request, CancellationToken ct)
        => (await _service.TurnInAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Hand the active contract to a stub contractor instead of clearing it yourself —
    /// escrows the reward as the contractor's fee.</summary>
    [HttpPost("{id:guid}/delegate")]
    public async Task<IActionResult> Delegate(Guid id, [FromBody] DelegateContractRequest request, CancellationToken ct)
        => (await _service.DelegateAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Report a contract failure (died / deadline expired) — only the INTENT; the
    /// server computes the reputation penalty from the contract's own stored reward and
    /// returns the resulting authoritative player state (ADR 0014).</summary>
    [HttpPost("{id:guid}/fail")]
    public async Task<IActionResult> Fail(Guid id, [FromBody] FailContractRequest request, CancellationToken ct)
        => (await _service.FailAsync(id, request, ct)).ToActionResult(this);
}
