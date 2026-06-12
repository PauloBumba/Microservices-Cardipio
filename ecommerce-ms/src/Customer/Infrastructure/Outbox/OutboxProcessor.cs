using Customer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
namespace Customer.Infrastructure.Outbox;
public sealed class OutboxProcessor(IServiceProvider services, ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Customer OutboxProcessor started.");
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
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var pub = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var msgs = await db.OutboxMessages.Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt).Take(20).ToListAsync(ct);
        foreach (var msg in msgs)
        {
            try
            {
                var type = Type.GetType(msg.Type)!;
                var payload = JsonSerializer.Deserialize(msg.Payload, type)!;
                await pub.Publish(payload, ct);
                msg.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex) { msg.RetryCount++; msg.Error = ex.Message; }
        }
        await db.SaveChangesAsync(ct);
    }
}
