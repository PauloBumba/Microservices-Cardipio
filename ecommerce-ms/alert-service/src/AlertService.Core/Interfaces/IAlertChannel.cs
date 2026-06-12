using AlertService.Core.Models;

namespace AlertService.Core.Interfaces;

public interface IAlertChannel
{
    string Name { get; }
    Task SendAsync(AlertNotification notification, CancellationToken ct = default);
}
