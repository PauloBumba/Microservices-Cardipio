using MediatR;
using Order.Application.Repositories;
using Shared.Application.Response;
namespace Order.Application.Features.Orders.Commands.CancelOrder;
public sealed class CancelOrderHandler(IOrderRepository repo)
    : IRequestHandler<CancelOrderCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return ApiResponse<bool>.Fail("Pedido não encontrado.");
        order.Cancel(cmd.Reason);
        return ApiResponse<bool>.Ok(true);
    }
}
