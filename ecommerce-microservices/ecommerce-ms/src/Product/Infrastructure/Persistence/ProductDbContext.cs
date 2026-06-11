using Microsoft.EntityFrameworkCore;
using Product.Domain.Primitives;
using Product.Domain.Repositories;
using Product.Infrastructure.Outbox;
using System.Text.Json;
namespace Product.Infrastructure.Persistence;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Domain.Entities.Productss> Products => Set<Domain.Entities.Productss>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);
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