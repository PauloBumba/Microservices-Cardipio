using MediatR;
using Microsoft.AspNetCore.Mvc;
using Product.Application.DTOs;
using Product.Application.Features.Products.Commands.AddStock;
using Product.Application.Features.Products.Commands.CreateProduct;
using Product.Application.Features.Products.Commands.ReserveStock;
using Product.Application.Features.Products.Queries.GetProductById;
using Product.Application.Features.Products.Queries.GetProducts;
namespace Product.Api.Controllers;
[ApiController]
[Route("api/v1/products")]
[Produces("application/json")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page=1, [FromQuery] int pageSize=20,
        [FromQuery] string? category=null, CancellationToken ct=default)
        => Ok(await sender.Send(new GetProductsQuery(page, pageSize, category), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct=default)
    {
        var r = await sender.Send(new GetProductByIdQuery(id), ct);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand cmd, CancellationToken ct=default)
    {
        var p = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = p.Id }, p);
    }

    [HttpPost("{id:guid}/stock/add")]
    [ProducesResponseType(typeof(StockInfoDto), 200)]
    public async Task<IActionResult> AddStock(Guid id, [FromBody] QuantityRequest req, CancellationToken ct=default)
        => Ok(await sender.Send(new AddStockCommand(id, req.Quantity), ct));

    [HttpPost("{id:guid}/stock/reserve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> ReserveStock(Guid id, [FromBody] QuantityRequest req, CancellationToken ct=default)
    {
        var ok = await sender.Send(new ReserveStockCommand(id, req.Quantity), ct);
        return ok ? Ok(new { reserved=true }) : Conflict(new { reserved=false, message="Estoque insuficiente." });
    }
}
public sealed record QuantityRequest(int Quantity);
