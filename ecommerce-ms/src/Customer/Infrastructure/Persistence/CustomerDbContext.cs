using Customer.Domain.Entities;
using Customer.Domain.Events;
using Customer.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Behaviors;
using Shared.Infrastructure.Outbox;
using Shared.Infrastructure.Logging.Repositories;
using Shared.IntegrationEvents;
using System.Linq;

namespace Customer.Infrastructure.Persistence;

public class CustomerDbContext(DbContextOptions<CustomerDbContext> options, TimeProvider timeProvider)
    : DbContext(options), IUnitOfWorkAccessor
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public DbSet<Customerss> Customers => Set<Customerss>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<CustomerProcessedEvent> ProcessedEvents => Set<CustomerProcessedEvent>();

    public async Task CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(CustomerDbContext).Assembly);
        mb.ConfigureAuditEntries();

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

    // Intercepta SaveChanges para converter Domain Events em Integration Events no Outbox
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ConvertDomainEventsToOutbox();
        return await base.SaveChangesAsync(ct);
    }

    private void ConvertDomainEventsToOutbox()
    {
        var integrationEvents = ChangeTracker.Entries<Shared.Domain.Primitives.AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents.ToList())
            .Select(e => ConvertToIntegrationEvent(e))
            .Where(ev => ev != null)
            .Select(ev => OutboxMessage.Create(
                $"{ev!.GetType().FullName}, {ev.GetType().Assembly.GetName().Name}",
                System.Text.Json.JsonSerializer.Serialize(ev, ev.GetType()),
                _timeProvider))
            .ToList();

        if (integrationEvents.Count > 0)
            OutboxMessages.AddRange(integrationEvents);

        foreach (var entry in ChangeTracker.Entries<Shared.Domain.Primitives.AggregateRoot>())
            entry.Entity.ClearDomainEvents();
    }

    private object? ConvertToIntegrationEvent(Shared.Domain.Primitives.IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            CustomerCreatedDomainEvent e => new CustomerCreatedIntegrationEvent(e.CustomerId, e.Name, e.Email),
            _ => null // Ignorar outros eventos que não precisam de integração
        };
    }
}
