using Notification.Domain.Entities;
namespace Notification.Domain.Repositories;
public interface INotificationRepository
{
    Task AddAsync(NotificationS notification, CancellationToken ct = default);
    Task<IEnumerable<NotificationS>> GetPagedAsync(int page, int size, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    void Update(NotificationS notification);
}
