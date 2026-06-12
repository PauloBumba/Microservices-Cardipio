using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Contracts;
using Order.Domain.Repositories;
using Order.Infrastructure.Http;
using Order.Infrastructure.Outbox;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Repositories;
using Polly;
using Polly.Extensions.Http;
namespace Order.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<OrderDbContext>(opt => opt.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrderDbContext>());

        // ── HttpClient + Polly ────────────────────────────────────────────────
        services.AddHttpClient<IProductServiceClient, ProductServiceClient>(c =>
                c.BaseAddress = new Uri(cfg["ProductService:BaseUrl"] ?? "http://product-service:8080"))
            .AddPolicyHandler(BuildRetryPolicy())
            .AddPolicyHandler(BuildCircuitBreakerPolicy());

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

    // Retry: 3x com backoff exponencial (2s, 4s, 8s)
    private static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy() =>
        HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (out_, ts, attempt, _) =>
                    Console.WriteLine($"[Polly Retry] tentativa {attempt} após {ts.TotalSeconds:F1}s"));

    // Circuit Breaker: abre após 5 falhas, permanece aberto por 1 minuto
    private static IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerPolicy() =>
        HttpPolicyExtensions.HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1),
                onBreak:    (_, ts) => Console.WriteLine($"[Polly CB] ABERTO por {ts.TotalSeconds:F0}s"),
                onReset:    ()      => Console.WriteLine("[Polly CB] FECHADO"),
                onHalfOpen: ()      => Console.WriteLine("[Polly CB] HALF-OPEN"));
}
