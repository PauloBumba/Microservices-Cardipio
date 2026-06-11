using MediatR;
using Order.Application.DTOs;
using Order.Application.Mappings;
using Order.Domain.Repositories;
namespace Order.Application.Features.Orders.Queries.GetOrderById;
public sealed class GetOrderByIdHandler(IOrderRepository repo) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery q, CancellationToken ct)
        => (await repo.GetByIdAsync(q.Id, ct))?.ToDto();
}
