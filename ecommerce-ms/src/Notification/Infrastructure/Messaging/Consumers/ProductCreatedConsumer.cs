using MassTransit;
using Microsoft.Extensions.Logging;
using Notification.Application.Contracts;
using Notification.Domain.Repositories;
using Shared.IntegrationEvents;

namespace Notification.Infrastructure.Messaging.Consumers;

public sealed class ProductCreatedConsumer(
    INotificationRepository repo,
    IUnitOfWork uow,
    ILogger<ProductCreatedConsumer> logger)
    : IConsumer<ProductCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> ctx)
    {
        var evt = ctx.Message;
        logger.LogInformation("[Notification] ProductCreated: {Sku} - {Name}", evt.Sku, evt.Name);

        var notification = Domain.Entities.NotificationS.Create(
            type: "ProductCreated",
            recipient: "admin@cardipio.com",
            channel: "Email",
            subject: $"Novo produto criado: {evt.Name}",
            body: $"Um novo produto foi cadastrado: {evt.Name} (SKU: {evt.Sku}) na categoria {evt.Category}");

        await repo.AddAsync(notification, ctx.CancellationToken);

        // Simulação de envio
        await Task.Delay(50, ctx.CancellationToken);
        notification.MarkSent();
        repo.Update(notification);

        await uow.CommitAsync(ctx.CancellationToken);
        logger.LogInformation("[Notification] Email simulado → produto {Sku}", evt.Sku);
    }
}
