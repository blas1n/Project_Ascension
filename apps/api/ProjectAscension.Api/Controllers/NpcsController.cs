using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/npcs")]
public class NpcsController : ControllerBase
{
    private readonly INpcRepository _repo;

    public NpcsController(INpcRepository repo) => _repo = repo;

    /// <summary>The city's NPC roster (read-only) — the MVP's static shop / guard /
    /// contract-clerk presence.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var npcs = await _repo.GetAllAsync(ct);
        return Ok(npcs.Select(n => new NpcResponse(n.Name, n.Role)));
    }
}
