using MediatR;
using Microsoft.Extensions.Logging;
using Order.Domain.Exceptions;
using Order.Domain.Repositories;
namespace Order.Application.Features.Orders.Commands.CancelOrder;
public sealed class CancelOrderHandler(IOrderRepository repo, IUnitOfWork uow, ILogger<CancelOrderHandler> log)
    : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await repo.GetByIdTrackedAsync(cmd.OrderId, ct) ?? throw new OrderNotFoundException(cmd.OrderId);
        order.Cancel(cmd.Reason);
        await uow.CommitAsync(ct);
        log.LogInformation("Pedido cancelado: {Id}", cmd.OrderId);
    }
}
