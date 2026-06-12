using Microsoft.EntityFrameworkCore;
using Notification.Domain.Repositories;
namespace Notification.Infrastructure.Persistence;
public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Domain.Entities.NotificationS> Notifications => Set<Domain.Entities.NotificationS>();
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
        base.OnModelCreating(mb);
    }
    public Task<int> CommitAsync(CancellationToken ct = default) => SaveChangesAsync(ct);
}
