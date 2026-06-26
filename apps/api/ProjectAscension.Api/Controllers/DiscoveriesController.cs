using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/discoveries")]
public class DiscoveriesController : ControllerBase
{
    private readonly IDiscoveryService _service;
    private readonly ISkillCompositionService _composition;
    private readonly IDiscoveryTuningProvider _tuning;

    public DiscoveriesController(IDiscoveryService service, ISkillCompositionService composition, IDiscoveryTuningProvider tuning)
    {
        _service = service;
        _composition = composition;
        _tuning = tuning;
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

    /// <summary>Scores a behavior signature; if it crosses the significance threshold
    /// the rule engine fires a discovery (ADR 0002 core 4 — a function, not a catalog).
    /// Returns whether it fired, the score, and the new discovery id.</summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateTriggerRequest request, CancellationToken ct)
    {
        var result = await _composition.EvaluateAndTriggerAsync(request, ct);
        return Ok(result);
    }

    /// <summary>The current discovery balance values (read-only) — for inspection and
    /// verifying runtime edits to the tuning rows.</summary>
    [HttpGet("tuning")]
    public async Task<IActionResult> GetTuning(CancellationToken ct)
    {
        var t = await _tuning.GetAsync(ct);
        return Ok(new DiscoveryTuningResponse(
            t.BehaviorWeights, t.FactorWeights, t.DefaultBehaviorWeight, t.DefaultFactorWeight,
            t.PersistenceWeight, t.CombinationSynergy, t.FireThreshold,
            t.BudgetBase, t.BudgetPerScore, t.BudgetMin, t.BudgetMax,
            t.UncommonScore, t.RareScore, t.EpicScore, t.LegendaryScore));
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
