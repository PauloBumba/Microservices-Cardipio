using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Contracts;
using Order.Application.DTOs;
using Order.Application.Mappings;
using Order.Domain.Entities;
using Order.Domain.Exceptions;
using Order.Domain.Repositories;
namespace Order.Application.Features.Orders.Commands.CreateOrder;
public sealed class CreateOrderHandler(
    IOrderRepository repo, IUnitOfWork uow,
    IProductServiceClient productClient, ILogger<CreateOrderHandler> log)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Orderss.Create(cmd.CustomerId,cmd.Street,cmd.City,cmd.State,cmd.ZipCode,cmd.Country,cmd.Notes);

        foreach (var line in cmd.Items)
        {
            var product = await productClient.GetProductAsync(line.ProductId, ct)
                ?? throw new OrderDomainException($"Produto {line.ProductId} não encontrado.");
            if (!product.IsActive)
                throw new OrderDomainException($"Produto '{product.Name}' está inativo.");
            var reserved = await productClient.ReserveStockAsync(line.ProductId, line.Quantity, ct);
            if (!reserved)
                throw new OrderDomainException($"Estoque insuficiente para '{product.Name}'.");
            order.AddItem(product.Id,product.Name,product.Sku,line.Quantity,product.Price,product.Currency);
        }

        order.Confirm();
        await repo.AddAsync(order, ct);
        await uow.CommitAsync(ct);
        log.LogInformation("Pedido criado: {OrderNumber}", order.OrderNumber);
        return order.ToDto();
    }
}
