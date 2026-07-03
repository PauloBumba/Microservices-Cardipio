using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Repositories;
using Shared.Application.Response;
using DomainOrder = Order.Domain.Entities.Orders;

namespace Order.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository repo,
    ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, ApiResponse<Guid>>
{
    public async Task<ApiResponse<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        if (cmd.Items.Count == 0)
            return ApiResponse<Guid>.Fail("O pedido deve ter ao menos um item.");

        var order = DomainOrder.Create(cmd.CustomerId);

        foreach (var item in cmd.Items)
            order.AddItem(item.ProductId, item.ProductName, item.Sku,
                item.Quantity, item.UnitPrice, item.Currency);

        await repo.AddAsync(order, ct);
        logger.LogInformation("Pedido criado: {Id} Nº={Number}", order.Id, order.OrderNumber);
        return ApiResponse<Guid>.Ok(order.Id);
    }
}
