using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Order.Application.Contracts;
using Order.Application.Repositories;
using Order.Infrastructure.Http;
using Order.Infrastructure.Outbox;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Repositories;
using Polly;
using Polly.Extensions.Http;
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

        services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
        {
            client.BaseAddress = new Uri(cfg["ProductService:BaseUrl"] ?? "http://product-service:8080");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(30));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
