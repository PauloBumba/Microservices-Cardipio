using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using Order.Infrastructure.Idempotency;
using Shared.Application.Behaviors;
using Shared.Infrastructure.Outbox;
using System.Text.Json;

namespace Order.Infrastructure.Persistence;

public class OrderDbContext(DbContextOptions<OrderDbContext> options, TimeProvider timeProvider)
    : DbContext(options), IUnitOfWorkAccessor
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public DbSet<Orders> Orders => Set<Orders>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OrderProcessedEvent> ProcessedEvents => Set<OrderProcessedEvent>();

    public async Task CommitAsync(CancellationToken ct = default) => await SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
        mb.Entity<OutboxMessage>(e => { e.ToTable("OutboxMessages"); e.HasKey(x => x.Id); e.Property(x => x.Status).HasConversion<string>(); });
        mb.Entity<OrderProcessedEvent>(e => { e.ToTable("ProcessedEvents"); e.HasKey(x => x.EventId); });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ConvertDomainEventsToOutbox();
        return await base.SaveChangesAsync(ct);
    }

    private void ConvertDomainEventsToOutbox()
    {
        var events = ChangeTracker.Entries<Shared.Domain.Primitives.AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents.ToList())
            .Select(ev => OutboxMessage.Create(
                $"{ev.GetType().FullName}, {ev.GetType().Assembly.GetName().Name}",
                JsonSerializer.Serialize(ev, ev.GetType()),
                _timeProvider)).ToList();
        if (events.Count > 0) OutboxMessages.AddRange(events);
        foreach (var entry in ChangeTracker.Entries<Shared.Domain.Primitives.AggregateRoot>())
            entry.Entity.ClearDomainEvents();
    }
}