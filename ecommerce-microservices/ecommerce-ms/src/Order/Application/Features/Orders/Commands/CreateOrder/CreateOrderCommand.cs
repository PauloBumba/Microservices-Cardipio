using MediatR;
using Order.Application.DTOs;
namespace Order.Application.Features.Orders.Commands.CreateOrder;
public sealed record CreateOrderCommand(
    Guid CustomerId, string Street, string City, string State,
    string ZipCode, string Country, string? Notes,
    IReadOnlyList<OrderLineDto> Items) : IRequest<OrderDto>;
public sealed record OrderLineDto(Guid ProductId, int Quantity);
