using MediatR;
using Order.Application.DTOs;
namespace Order.Application.Features.Orders.Queries.GetOrdersByCustomer;
public sealed record GetOrdersByCustomerQuery(Guid CustomerId, int Page=1, int PageSize=20)
    : IRequest<PagedResult<OrderDto>>;
