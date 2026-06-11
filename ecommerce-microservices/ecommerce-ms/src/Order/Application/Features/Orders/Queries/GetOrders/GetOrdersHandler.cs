using MediatR;
using Order.Application.DTOs;
using Order.Application.Mappings;
using Order.Domain.Repositories;
namespace Order.Application.Features.Orders.Queries.GetOrders;
public sealed class GetOrdersHandler(IOrderRepository repo) : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery q, CancellationToken ct)
    {
        var items = await repo.GetPagedAsync(q.Page, q.PageSize, ct);
        var total = await repo.CountAsync(ct);
        return new PagedResult<OrderDto>(items.Select(o => o.ToDto()).ToList(), total, q.Page, q.PageSize);
    }
}
