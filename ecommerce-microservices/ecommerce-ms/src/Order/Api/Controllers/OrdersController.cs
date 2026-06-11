using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.Application.DTOs;
using Order.Application.Features.Orders.Commands.CancelOrder;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Features.Orders.Queries.GetOrderById;
using Order.Application.Features.Orders.Queries.GetOrders;
using Order.Application.Features.Orders.Queries.GetOrdersByCustomer;
namespace Order.Api.Controllers;
[ApiController]
[Route("api/v1/orders")]
[Produces("application/json")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page=1, [FromQuery] int pageSize=20, CancellationToken ct=default)
        => Ok(await sender.Send(new GetOrdersQuery(page, pageSize), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct=default)
    {
        var r = await sender.Send(new GetOrderByIdQuery(id), ct);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), 200)]
    public async Task<IActionResult> GetByCustomer(Guid customerId,
        [FromQuery] int page=1, [FromQuery] int pageSize=20, CancellationToken ct=default)
        => Ok(await sender.Send(new GetOrdersByCustomerQuery(customerId, page, pageSize), ct));

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand cmd, CancellationToken ct=default)
    {
        var order = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRequest req, CancellationToken ct=default)
    {
        await sender.Send(new CancelOrderCommand(id, req.Reason), ct);
        return NoContent();
    }
}
public sealed record CancelRequest(string Reason);
