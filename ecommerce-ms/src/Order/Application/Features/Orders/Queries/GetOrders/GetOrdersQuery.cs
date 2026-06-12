using MediatR;
using Order.Application.DTOs;
namespace Order.Application.Features.Orders.Queries.GetOrders;
public sealed record GetOrdersQuery(int Page=1, int PageSize=20) : IRequest<PagedResult<OrderDto>>;
