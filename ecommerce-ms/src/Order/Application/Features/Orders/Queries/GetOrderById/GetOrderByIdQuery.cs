using MediatR;
using Order.Application.DTOs;
using Shared.Application.Caching;
using Shared.Application.Response;
namespace Order.Application.Features.Orders.Queries.GetOrderById;
public sealed record GetOrderByIdQuery(Guid Id) : IRequest<ApiResponse<OrderDto>>, ICacheableQuery
{
    public string CacheKey => $"orders:{Id}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
