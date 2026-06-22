using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Services;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/characters")]
public class CharactersController : ControllerBase
{
    private readonly ICharacterService _service;
    public CharactersController(ICharacterService service) => _service = service;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
