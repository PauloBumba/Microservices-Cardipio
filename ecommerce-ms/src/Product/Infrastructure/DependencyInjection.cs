using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Application.Repositories;
using Product.Infrastructure.Outbox;
using Product.Infrastructure.Persistence;
using Product.Infrastructure.Repositories;
using Shared.Application.Behaviors;

namespace Product.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<ProductDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

        services.AddScoped<IUnitOfWorkAccessor>(sp => sp.GetRequiredService<ProductDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddHostedService<ProductOutboxProcessor>();

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
