using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Extensions;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Interfaces;

namespace ProjectAscension.Api.Controllers;

[ApiController]
[Route("api/shop")]
public class ShopController : ControllerBase
{
    private readonly IItemDefinitionRepository _repo;
    private readonly IShopService _service;

    public ShopController(IItemDefinitionRepository repo, IShopService service)
    {
        _repo = repo;
        _service = service;
    }

    /// <summary>The city shop's item catalog with buy/sell prices (read-only). The client
    /// fetches these, so a balance edit retunes the resource economy with no rebuild.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _repo.GetAllAsync(ct);
        return Ok(items.Select(i => new ItemDefinitionResponse(i.Key, i.DisplayName, i.Description, i.SellPrice, i.BuyPrice)));
    }

    /// <summary>Buy an item from the shop — the price always comes from the server's own
    /// item catalog, never the request (ADR 0014).</summary>
    [HttpPost("buy")]
    public async Task<IActionResult> Buy([FromBody] BuyItemRequest request, CancellationToken ct)
        => (await _service.BuyAsync(request, ct)).ToActionResult(this);

    /// <summary>Sell an item to the shop.</summary>
    [HttpPost("sell")]
    public async Task<IActionResult> Sell([FromBody] SellItemRequest request, CancellationToken ct)
        => (await _service.SellAsync(request, ct)).ToActionResult(this);
}
