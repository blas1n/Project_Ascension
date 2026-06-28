using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/monsters")]
public class MonstersController : ControllerBase
{
    private readonly IMonsterDefinitionRepository _repo;

    public MonstersController(IMonsterDefinitionRepository repo) => _repo = repo;

    /// <summary>The monster stat definitions (read-only). The client fetches these and
    /// builds its monsters, so a balance edit (or, later, an AI/dynamic system) retunes
    /// them with no client rebuild.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var monsters = await _repo.GetAllAsync(ct);
        return Ok(monsters.Select(m => new MonsterDefinitionResponse(
            m.Key, m.MaxHealth, m.MoveSpeed, m.AggroRange, m.AttackRange,
            m.AttackCooldown, m.Damage, m.ProjectileSpeed, m.Scale, m.DropItemKey, m.DropAmount)));
    }
}
