using Customer.Domain.Entities;
using Customer.Infrastructure.Idempotency;
using Customer.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Behaviors;
using Shared.Infrastructure.Outbox;

namespace Customer.Infrastructure.Persistence;

public class CustomerDbContext(DbContextOptions<CustomerDbContext> options)
    : DbContext(options), IUnitOfWorkAccessor
{
    public DbSet<Customerss> Customers => Set<Customerss>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<CustomerProcessedEvent> ProcessedEvents => Set<CustomerProcessedEvent>();

    public async Task CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(CustomerDbContext).Assembly);

        mb.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
        });

        mb.Entity<CustomerProcessedEvent>(e =>
        {
            e.ToTable("ProcessedEvents");
            e.HasKey(x => x.EventId);
        });
    }

    // Intercepta SaveChanges para serializar Domain Events no Outbox
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ConvertDomainEventsToOutbox();
        return await base.SaveChangesAsync(ct);
    }

    private void ConvertDomainEventsToOutbox()
    {
        var events = ChangeTracker.Entries<Shared.Domain.Primitives.AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .Select(ev => new OutboxMessage
            {
                Type = $"{ev.GetType().FullName}, {ev.GetType().Assembly.GetName().Name}",
                Payload = System.Text.Json.JsonSerializer.Serialize(ev, ev.GetType())
            })
            .ToList();

        if (events.Count > 0)
            OutboxMessages.AddRange(events);

        foreach (var entry in ChangeTracker.Entries<Shared.Domain.Primitives.AggregateRoot>())
            entry.Entity.ClearDomainEvents();
    }
}
