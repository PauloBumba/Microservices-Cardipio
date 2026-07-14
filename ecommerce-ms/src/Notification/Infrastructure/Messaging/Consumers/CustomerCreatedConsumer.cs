using MassTransit;
using Microsoft.Extensions.Logging;
using Notification.Application.Contracts;
using Notification.Domain.Repositories;
using Shared.IntegrationEvents;
namespace Notification.Infrastructure.Messaging.Consumers;
public sealed class CustomerCreatedConsumer(
    INotificationRepository repo, IUnitOfWork uow,
    ILogger<CustomerCreatedConsumer> logger)
    : IConsumer<CustomerCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CustomerCreatedIntegrationEvent> ctx)
    {
        var evt = ctx.Message;
        logger.LogInformation("[Notification] CustomerCreated: {Email}", evt.Email);

        var notification = Domain.Entities.NotificationS.Create(
            type:      "CustomerCreated",
            recipient: evt.Email,
            channel:   "Email",
            subject:   "Bem-vindo(a)!",
            body:      $"Olá {evt.Name}, seu cadastro foi realizado com sucesso!");

        await repo.AddAsync(notification, ctx.CancellationToken);
        await Task.Delay(50, ctx.CancellationToken);
        notification.MarkSent();
        repo.Update(notification);
        await uow.CommitAsync(ctx.CancellationToken);
        logger.LogInformation("[Notification] Email simulado → {Email}", evt.Email);
    }
}
