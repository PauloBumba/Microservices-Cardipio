using Customer.Application.DTOs;
using Customer.Application.Features.Customers.Commands.CreateCustomer;
using Customer.Application.Features.Customers.Commands.DeactivateCustomer;
using Customer.Application.Features.Customers.Commands.UpdateCustomer;
using Customer.Application.Features.Customers.Queries.GetCustomerById;
using Customer.Application.Features.Customers.Queries.GetCustomers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Customer.Api.Controllers;
[ApiController]
[Route("api/v1/customers")]
[Produces("application/json")]
public sealed class CustomersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page=1, [FromQuery] int pageSize=20, CancellationToken ct=default)
        => Ok(await sender.Send(new GetCustomersQuery(page, pageSize), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct=default)
    {
        var r = await sender.Send(new GetCustomerByIdQuery(id), ct);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand cmd, CancellationToken ct=default)
    {
        var c = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, c);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerCommand cmd, CancellationToken ct=default)
        => Ok(await sender.Send(cmd with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct=default)
    {
        await sender.Send(new DeactivateCustomerCommand(id), ct);
        return NoContent();
    }
}
