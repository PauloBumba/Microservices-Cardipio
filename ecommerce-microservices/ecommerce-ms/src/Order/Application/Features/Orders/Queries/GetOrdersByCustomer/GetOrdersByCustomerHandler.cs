using MediatR;
using Order.Application.DTOs;
using Order.Application.Mappings;
using Order.Domain.Repositories;
namespace Order.Application.Features.Orders.Queries.GetOrdersByCustomer;
public sealed class GetOrdersByCustomerHandler(IOrderRepository repo)
    : IRequestHandler<GetOrdersByCustomerQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersByCustomerQuery q, CancellationToken ct)
    {
        var items = await repo.GetByCustomerAsync(q.CustomerId, q.Page, q.PageSize, ct);
        var total = await repo.CountByCustomerAsync(q.CustomerId, ct);
        return new PagedResult<OrderDto>(items.Select(o => o.ToDto()).ToList(), total, q.Page, q.PageSize);
    }
}
