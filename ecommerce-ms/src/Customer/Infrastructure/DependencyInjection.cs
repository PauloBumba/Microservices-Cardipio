using Customer.Application.Repositories;
using Customer.Infrastructure.Outbox;
using Customer.Infrastructure.Persistence;
using Customer.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Behaviors;

namespace Customer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        // ── Banco de dados ────────────────────────────────────────────────
        services.AddDbContext<CustomerDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));
        
        // ── UoW (TransactionBehavior usa esta interface) ───────────────────
        services.AddScoped<IUnitOfWorkAccessor>(sp => sp.GetRequiredService<CustomerDbContext>());

        // ── Repositórios ──────────────────────────────────────────────────
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        // ── Outbox Processor melhorado ────────────────────────────────────
        services.AddHostedService<CustomerOutboxProcessor>();

        // ── Cache distribuído (Redis ou in-memory) ────────────────────────
        var redis = cfg.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redis))
            services.AddStackExchangeRedisCache(o => o.Configuration = redis);
        else
            services.AddDistributedMemoryCache();

        // ── MassTransit + RabbitMQ ────────────────────────────────────────
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
