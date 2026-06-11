using Customer.Domain.Repositories;
using Customer.Infrastructure.Outbox;
using Customer.Infrastructure.Persistence;
using Customer.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Customer.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<CustomerDbContext>(opt => opt.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CustomerDbContext>());
        services.AddHostedService<OutboxProcessor>();
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
