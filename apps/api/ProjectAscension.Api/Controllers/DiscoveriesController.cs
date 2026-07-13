using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/discoveries")]
public class DiscoveriesController : ControllerBase
{
    private static readonly Error UnknownActor = new(
        "UNKNOWN_ACTOR", "No character exists for this actor id. Create a character first.");

    private readonly IDiscoveryService _service;
    private readonly ISkillCompositionService _composition;
    private readonly IDiscoveryTuningProvider _tuning;
    private readonly ICharacterService _characters;

    public DiscoveriesController(
        IDiscoveryService service, ISkillCompositionService composition, IDiscoveryTuningProvider tuning,
        ICharacterService characters)
    {
        _service = service;
        _composition = composition;
        _tuning = tuning;
        _characters = characters;
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
        // A discovery FK's DiscovererActorId to Actors — an unknown actor is a client error
        // (never created a character), not a server crash.
        if (!await _characters.ActorExistsAsync(request.ActorId, ct)) return BadRequest(UnknownActor);

        var discoveryId = await _composition.TriggerAsync(request, ct);
        return AcceptedAtAction(nameof(GetSkill), new { discoveryId }, new { discoveryId, status = "Pending" });
    }

    /// <summary>Scores a behavior signature; if it crosses the significance threshold
    /// the rule engine fires a discovery (ADR 0002 core 4 — a function, not a catalog).
    /// Returns whether it fired, the score, and the new discovery id.
    /// NOTE (ADR 0004): in production the trigger is fed by the authoritative server
    /// simulation observing behavior directly; this client-facing REST entry point is
    /// slice scaffolding for tooling and the no-server-sim slice.</summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateTriggerRequest request, CancellationToken ct)
    {
        // A fresh player has no Actor row yet (the client only ever gets one from character
        // creation). Firing a discovery for an unknown actor would FK-violate on insert — catch
        // it here as a clear 4xx instead of a 500 (a missing actor is a client error).
        if (!await _characters.ActorExistsAsync(request.ActorId, ct)) return BadRequest(UnknownActor);

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
            t.KnowledgeDepthWeight, t.PersistenceWeight, t.CombinationSynergy, t.FireThreshold,
            t.BudgetBase, t.BudgetGrowth, t.BudgetMin, t.BudgetMax,
            t.UncommonScore, t.RareScore, t.EpicScore, t.LegendaryScore));
    }

    /// <summary>The discovery's recorded lineage — the prior discoveries it was built
    /// on (discovery.md 발견 계보), nearest first.</summary>
    [HttpGet("{discoveryId:guid}/lineage")]
    public async Task<IActionResult> GetLineage(Guid discoveryId, CancellationToken ct)
        => Ok(await _composition.GetLineageAsync(discoveryId, ct));

    /// <summary>Polls a discovery's content: Pending until the AI composes it, then
    /// the frozen skill.</summary>
    [HttpGet("{discoveryId:guid}/skill")]
    public async Task<IActionResult> GetSkill(Guid discoveryId, CancellationToken ct)
    {
        var skill = await _composition.GetByDiscoveryAsync(discoveryId, ct);
        return skill is null ? NotFound() : Ok(skill);
    }
}
