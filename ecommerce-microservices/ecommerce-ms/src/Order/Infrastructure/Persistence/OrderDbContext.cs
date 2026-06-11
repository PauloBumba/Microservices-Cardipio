using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using Order.Domain.Primitives;
using Order.Domain.Repositories;
using Order.Infrastructure.Outbox;
using System.Text.Json;
namespace Order.Infrastructure.Persistence;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Order.Domain.Entities.Orderss> Orders => Set<Order.Domain.Entities.Orderss>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
        base.OnModelCreating(mb);
    }
    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        var aggs = ChangeTracker.Entries<Entity>().Where(e => e.Entity.DomainEvents.Any()).Select(e => e.Entity).ToList();
        foreach (var agg in aggs)
        {
            foreach (var ev in agg.DomainEvents)
                OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = ev.GetType().AssemblyQualifiedName!,
                    Payload = JsonSerializer.Serialize(ev, ev.GetType()),
                    CreatedAt = DateTime.UtcNow
                });
            agg.ClearDomainEvents();
        }
        return await SaveChangesAsync(ct);
    }
}