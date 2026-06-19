using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using Product.Infrastructure.Idempotency;
using Shared.Application.Behaviors;
using Shared.Infrastructure.Outbox;
using System.Text.Json;

namespace Product.Infrastructure.Persistence;

public class ProductDbContext(DbContextOptions<ProductDbContext> options)
    : DbContext(options), IUnitOfWorkAccessor
{
    public DbSet<Productss> Products => Set<Productss>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProductProcessedEvent> ProcessedEvents => Set<ProductProcessedEvent>();

    public async Task CommitAsync(CancellationToken ct = default) => await SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);
        mb.Entity<OutboxMessage>(e => { e.ToTable("OutboxMessages"); e.HasKey(x => x.Id); e.Property(x => x.Status).HasConversion<string>(); });
        mb.Entity<ProductProcessedEvent>(e => { e.ToTable("ProcessedEvents"); e.HasKey(x => x.EventId); });
    }

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
                Payload = JsonSerializer.Serialize(ev, ev.GetType())
            }).ToList();

        if (events.Count > 0) OutboxMessages.AddRange(events);

        foreach (var entry in ChangeTracker.Entries<Shared.Domain.Primitives.AggregateRoot>())
            entry.Entity.ClearDomainEvents();
    }
}
