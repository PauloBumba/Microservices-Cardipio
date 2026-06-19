using MediatR;
using Order.Application.Repositories;
using Shared.Application.Response;
namespace Order.Application.Features.Orders.Commands.ConfirmOrder;
public sealed class ConfirmOrderHandler(IOrderRepository repo)
    : IRequestHandler<ConfirmOrderCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(ConfirmOrderCommand cmd, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return ApiResponse<bool>.Fail("Pedido não encontrado.");
        order.Confirm();
        return ApiResponse<bool>.Ok(true);
    }
}
