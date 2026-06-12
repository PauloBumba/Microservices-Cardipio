using Customer.Domain.Primitives;
using Customer.Domain.Repositories;
using Customer.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace Customer.Infrastructure.Persistence
{
    public sealed class CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : DbContext(options), IUnitOfWork
    {
        public DbSet<Domain.Entities.Customerss> Customers => Set<Domain.Entities.Customerss>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.ApplyConfigurationsFromAssembly(typeof(CustomerDbContext).Assembly);
            base.OnModelCreating(mb);
        }

        public async Task<int> CommitAsync(CancellationToken ct = default)
        {
            var aggregates = ChangeTracker.Entries<Entity>()
                .Where(e => e.Entity.DomainEvents.Any()).Select(e => e.Entity).ToList();
            foreach (var agg in aggregates)
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

}