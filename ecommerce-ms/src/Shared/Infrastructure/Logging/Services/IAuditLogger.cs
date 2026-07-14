using Shared.Infrastructure.Logging.Categories;

namespace Shared.Infrastructure.Logging.Services;

public interface IAuditLogger
{
    Task LogAsync(AuditLog auditLog, CancellationToken ct = default);
    Task LogLoginAsync(string userId, string userName, string ipAddress, bool success, CancellationToken ct = default);
    Task LogDataAccessAsync(string userId, string resource, string resourceId, string action, CancellationToken ct = default);
    Task LogConfigurationChangeAsync(string userId, string setting, string oldValue, string newValue, CancellationToken ct = default);
}
