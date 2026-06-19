using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Idempotency;
using Order.Infrastructure.Persistence;
using Shared.Infrastructure.Outbox;

namespace Order.Infrastructure.Outbox;

public sealed class OrderOutboxProcessor(IServiceProvider services, ILogger<OrderOutboxProcessor> logger)
    : OutboxProcessorBase(services, logger)
{
    protected override async Task<List<OutboxMessage>> GetPendingAsync(IServiceProvider sp, int batchSize, CancellationToken ct) =>
        await sp.GetRequiredService<OrderDbContext>().OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending && !m.IsProcessing)
            .OrderBy(m => m.CreatedAt).Take(batchSize).ToListAsync(ct);

    protected override async Task MarkProcessedAsync(IServiceProvider sp, OutboxMessage msg, CancellationToken ct)
    { msg.ProcessedAt = DateTime.UtcNow; msg.Status = OutboxMessageStatus.Processed; msg.IsProcessing = false; await sp.GetRequiredService<OrderDbContext>().SaveChangesAsync(ct); }

    protected override async Task IncrementRetryAsync(IServiceProvider sp, OutboxMessage msg, string error, CancellationToken ct)
    { msg.Error = error; msg.IsProcessing = false; await sp.GetRequiredService<OrderDbContext>().SaveChangesAsync(ct); }

    protected override async Task<bool> TryLockAsync(IServiceProvider sp, Guid messageId, CancellationToken ct)
    {
        var db = sp.GetRequiredService<OrderDbContext>();
        var msg = await db.OutboxMessages.FindAsync([messageId], ct);
        if (msg is null || msg.IsProcessing) return false;
        msg.IsProcessing = true; msg.LockedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return true;
    }

    protected override async Task<bool> WasProcessedAsync(IServiceProvider sp, Guid eventId, CancellationToken ct) =>
        await sp.GetRequiredService<OrderDbContext>().ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    protected override async Task RecordProcessedEventAsync(IServiceProvider sp, Guid eventId, string type, CancellationToken ct)
    {
        var db = sp.GetRequiredService<OrderDbContext>();
        try { db.ProcessedEvents.Add(new OrderProcessedEvent { EventId = eventId, EventType = type }); await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { }
    }
}
