using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Persistence;
using System.Text.Json;
namespace Order.Infrastructure.Outbox;
public sealed class OutboxProcessor(IServiceProvider services, ILogger<OutboxProcessor> logger, TimeProvider timeProvider) : BackgroundService
{
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Order OutboxProcessor started.");
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(ct); }
            catch (Exception ex) { logger.LogError(ex, "Outbox error"); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db  = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var msgs = await db.OutboxMessages.Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt).Take(20).ToListAsync(ct);
        foreach (var msg in msgs)
        {
            try
            {
                var type = Type.GetType(msg.Type)!;
                await bus.Publish(JsonSerializer.Deserialize(msg.Payload, type)!, type, ct);
                msg.ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime;
                logger.LogInformation("Outbox sent: {Type}", msg.Type);
            }
            catch (Exception ex) { msg.RetryCount++; msg.Error = ex.Message; }
        }
        await db.SaveChangesAsync(ct);
    }
}
