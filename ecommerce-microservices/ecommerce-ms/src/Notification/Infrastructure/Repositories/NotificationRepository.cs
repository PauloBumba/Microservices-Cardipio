using Microsoft.EntityFrameworkCore;
using Notification.Domain.Repositories;
using Notification.Infrastructure.Persistence;
namespace Notification.Infrastructure.Repositories;
public sealed class NotificationRepository(NotificationDbContext db) : INotificationRepository
{
    public async Task AddAsync(Domain.Entities.NotificationS n, CancellationToken ct = default)
        => await db.Notifications.AddAsync(n, ct);
    public async Task<IEnumerable<Domain.Entities.NotificationS>> GetPagedAsync(int page, int size, CancellationToken ct = default)
        => await db.Notifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedAt).Skip((page-1)*size).Take(size).ToListAsync(ct);
    public async Task<int> CountAsync(CancellationToken ct = default) => await db.Notifications.CountAsync(ct);
    public void Update(Domain.Entities.NotificationS n) => db.Notifications.Update(n);
}
