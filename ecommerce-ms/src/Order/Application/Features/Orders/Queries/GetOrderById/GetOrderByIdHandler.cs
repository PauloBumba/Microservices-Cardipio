using MediatR;
using Order.Application.DTOs;
using Order.Application.Repositories;
using Shared.Application.Response;
namespace Order.Application.Features.Orders.Queries.GetOrderById;
public sealed class GetOrderByIdHandler(IOrderRepository repo)
    : IRequestHandler<GetOrderByIdQuery, ApiResponse<OrderDto>>
{
    public async Task<ApiResponse<OrderDto>> Handle(GetOrderByIdQuery q, CancellationToken ct)
    {
        var o = await repo.GetByIdAsync(q.Id, ct);
        if (o is null) return ApiResponse<OrderDto>.Fail("Pedido não encontrado.");
        return ApiResponse<OrderDto>.Ok(new OrderDto(o.Id, o.OrderNumber, o.CustomerId,
            o.Status.ToString(), o.Total, o.Currency,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Sku,
                i.Quantity, i.UnitPrice, i.Subtotal)).ToList(),
            o.CreatedAt));
    }
}
