using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Domain.Repositories;
using Product.Infrastructure.Outbox;
using Product.Infrastructure.Persistence;
using Product.Infrastructure.Repositories;
namespace Product.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<ProductDbContext>(opt => opt.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProductDbContext>());
        services.AddStackExchangeRedisCache(opt => opt.Configuration = cfg["Redis:ConnectionString"] ?? "redis:6379");
        services.AddHostedService<OutboxProcessor>();
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.UsingRabbitMq((ctx, c) =>
            {
                c.Host(cfg["RabbitMQ:Host"] ?? "rabbitmq", "/", h =>
                { h.Username(cfg["RabbitMQ:Username"] ?? "rabbit"); h.Password(cfg["RabbitMQ:Password"] ?? "rabbit123"); });
                c.UseMessageRetry(r => r.Intervals(1000, 5000, 15000));
                c.ConfigureEndpoints(ctx);
            });
        });
        return services;
    }
}
