using Customer.Infrastructure.Idempotency;
using Customer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Outbox;
using OutboxMessage = Shared.Infrastructure.Outbox.OutboxMessage;

namespace Customer.Infrastructure.Outbox;

public sealed class CustomerOutboxProcessor(IServiceProvider services, ILogger<CustomerOutboxProcessor> logger, TimeProvider timeProvider)
    : OutboxProcessorBase(services, logger)
{
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task<List<OutboxMessage>> GetPendingAsync(IServiceProvider sp, int batchSize, CancellationToken ct)
    {
        var db = sp.GetRequiredService<CustomerDbContext>();
        return await db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending && !m.IsProcessing)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    protected override async Task MarkProcessedAsync(IServiceProvider sp, OutboxMessage msg, CancellationToken ct)
    {
        var db = sp.GetRequiredService<CustomerDbContext>();
        var entity = await db.OutboxMessages.FindAsync([msg.Id], ct);
        if (entity is not null)
        {
            entity.ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime;
            entity.Status = OutboxMessageStatus.Processed;
            entity.IsProcessing = false;
            await db.SaveChangesAsync(ct);
        }
    }

    protected override async Task IncrementRetryAsync(IServiceProvider sp, OutboxMessage msg, string error, CancellationToken ct)
    {
        var db = sp.GetRequiredService<CustomerDbContext>();
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
        var db = sp.GetRequiredService<CustomerDbContext>();
        var msg = await db.OutboxMessages.FindAsync([messageId], ct);
        if (msg is null || msg.IsProcessing) return false;
        msg.IsProcessing = true;
        msg.LockedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return true;
    }

    protected override async Task<bool> WasProcessedAsync(IServiceProvider sp, Guid eventId, CancellationToken ct)
    {
        var db = sp.GetRequiredService<CustomerDbContext>();
        return await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);
    }

    protected override async Task RecordProcessedEventAsync(IServiceProvider sp, Guid eventId, string type, CancellationToken ct)
    {
        var db = sp.GetRequiredService<CustomerDbContext>();
        // ON CONFLICT DO NOTHING via tentativa de inserção
        try
        {
            db.ProcessedEvents.Add(new CustomerProcessedEvent 
            { 
                EventId = eventId, 
                EventType = type,
                ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime
            });
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) { /* já existia — idempotência garantida */ }
    }
}
