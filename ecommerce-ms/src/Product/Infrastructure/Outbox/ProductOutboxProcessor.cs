using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Product.Infrastructure.Idempotency;
using Product.Infrastructure.Persistence;
using Shared.Infrastructure.Outbox;
using OutboxMessage = Shared.Infrastructure.Outbox.OutboxMessage;

namespace Product.Infrastructure.Outbox;

public sealed class ProductOutboxProcessor(IServiceProvider services, ILogger<ProductOutboxProcessor> logger)
    : OutboxProcessorBase(services, logger)
{
    protected override async Task<List<OutboxMessage>> GetPendingAsync(IServiceProvider sp, int batchSize, CancellationToken ct) =>
        await sp.GetRequiredService<ProductDbContext>().OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending && !m.IsProcessing)
            .OrderBy(m => m.CreatedAt).Take(batchSize).ToListAsync(ct);

    protected override async Task MarkProcessedAsync(IServiceProvider sp, OutboxMessage msg, CancellationToken ct)
    {
        var db = sp.GetRequiredService<ProductDbContext>();
        var entity = await db.OutboxMessages.FindAsync([msg.Id], ct);
        if (entity is not null)
        {
            entity.ProcessedAt = DateTime.UtcNow;
            entity.Status = OutboxMessageStatus.Processed;
            entity.IsProcessing = false;
            await db.SaveChangesAsync(ct);
        }
    }

    protected override async Task IncrementRetryAsync(IServiceProvider sp, OutboxMessage msg, string error, CancellationToken ct)
    {
        var db = sp.GetRequiredService<ProductDbContext>();
        var entity = await db.OutboxMessages.FindAsync([msg.Id], ct);
        if (entity is not null)
        {
            entity.Error = error;
            entity.IsProcessing = false;
            await db.SaveChangesAsync(ct);
        }
    }

    protected override async Task<bool> TryLockAsync(IServiceProvider sp, Guid messageId, CancellationToken ct)
    {
        var db = sp.GetRequiredService<ProductDbContext>();
        var msg = await db.OutboxMessages.FindAsync([messageId], ct);
        if (msg is null || msg.IsProcessing) return false;
        msg.IsProcessing = true; msg.LockedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return true;
    }

    protected override async Task<bool> WasProcessedAsync(IServiceProvider sp, Guid eventId, CancellationToken ct) =>
        await sp.GetRequiredService<ProductDbContext>().ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    protected override async Task RecordProcessedEventAsync(IServiceProvider sp, Guid eventId, string type, CancellationToken ct)
    {
        var db = sp.GetRequiredService<ProductDbContext>();
        try { db.ProcessedEvents.Add(new ProductProcessedEvent { EventId = eventId, EventType = type }); await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { /* idempotência */ }
    }
}
