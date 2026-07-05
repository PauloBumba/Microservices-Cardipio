using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Domain.Repositories;
using Notification.Infrastructure.Messaging.Consumers;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Repositories;
namespace Notification.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<NotificationDbContext>(opt => opt.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NotificationDbContext>());
       
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<OrderCreatedConsumer>();
            x.AddConsumer<CustomerCreatedConsumer>();

            x.UsingRabbitMq((ctx, c) =>
            {
                c.Host(cfg["RabbitMQ:Host"] ?? "rabbitmq", "/", h =>
                { h.Username(cfg["RabbitMQ:Username"] ?? "rabbit"); h.Password(cfg["RabbitMQ:Password"] ?? "rabbit123"); });

                c.ReceiveEndpoint("notification-order-created", e =>
                {
                    e.ConfigureConsumer<OrderCreatedConsumer>(ctx);
                    e.UseMessageRetry(r => r.Intervals(1000, 5000, 30_000));
                    e.BindDeadLetterQueue("notification-order-created-dlq");
                });

                c.ReceiveEndpoint("notification-customer-created", e =>
                {
                    e.ConfigureConsumer<CustomerCreatedConsumer>(ctx);
                    e.UseMessageRetry(r => r.Intervals(1000, 5000, 30_000));
                    e.BindDeadLetterQueue("notification-customer-created-dlq");
                });
            });
        });

        return services;
    }
}
