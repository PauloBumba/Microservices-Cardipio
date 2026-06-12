using MassTransit;
using Microsoft.Extensions.Logging;
using Notification.Application.Contracts;
using Notification.Domain.Repositories;
namespace Notification.Infrastructure.Messaging.Consumers;
public sealed class OrderCreatedConsumer(
    INotificationRepository repo, IUnitOfWork uow,
    ILogger<OrderCreatedConsumer> logger)
    : IConsumer<OrderCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> ctx)
    {
        var evt = ctx.Message;
        logger.LogInformation("[Notification] OrderCreated: {OrderNumber}", evt.OrderNumber);

        var notification = Domain.Entities.NotificationS.Create(
            type:      "OrderCreated",
            recipient: evt.CustomerId.ToString(),
            channel:   "Email",
            subject:   $"Pedido {evt.OrderNumber} confirmado!",
            body:      $"Olá! Seu pedido {evt.OrderNumber} no valor de {evt.Currency} {evt.TotalAmount:F2} foi recebido e está sendo processado.");

        await repo.AddAsync(notification, ctx.CancellationToken);

        // Simulação de envio
        await Task.Delay(50, ctx.CancellationToken);
        notification.MarkSent();
        repo.Update(notification);

        await uow.CommitAsync(ctx.CancellationToken);
        logger.LogInformation("[Notification] Email simulado → pedido {OrderNumber}", evt.OrderNumber);
    }
}
