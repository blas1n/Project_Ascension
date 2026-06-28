using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/shop")]
public class ShopController : ControllerBase
{
    private readonly IItemDefinitionRepository _repo;

    public ShopController(IItemDefinitionRepository repo) => _repo = repo;

    /// <summary>The city shop's item catalog with buy/sell prices (read-only). The client
    /// fetches these, so a balance edit retunes the resource economy with no rebuild.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _repo.GetAllAsync(ct);
        return Ok(items.Select(i => new ItemDefinitionResponse(i.Key, i.DisplayName, i.SellPrice, i.BuyPrice)));
    }
}
