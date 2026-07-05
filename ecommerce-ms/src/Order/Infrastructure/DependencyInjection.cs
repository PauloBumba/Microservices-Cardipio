using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Repositories;
using Order.Infrastructure.Outbox;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Repositories;
using Shared.Application.Behaviors;

namespace Order.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<OrderDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

        services.AddScoped<IUnitOfWorkAccessor>(sp => sp.GetRequiredService<OrderDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddHostedService<OrderOutboxProcessor>();
        
        var redis = cfg.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redis))
            services.AddStackExchangeRedisCache(o => o.Configuration = redis);
        else
            services.AddDistributedMemoryCache();

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.UsingRabbitMq((ctx, c) =>
            {
                c.Host(cfg["RabbitMQ:Host"] ?? "rabbitmq", "/", h =>
                {
                    h.Username(cfg["RabbitMQ:Username"] ?? "rabbit");
                    h.Password(cfg["RabbitMQ:Password"] ?? "rabbit123");
                });
                c.UseMessageRetry(r => r.Intervals(1000, 5000, 15000));
                c.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
