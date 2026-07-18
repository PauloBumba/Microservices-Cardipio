using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Logging.Categories;
using Shared.Application.Auditing;

namespace Shared.Infrastructure.Logging.Services;

public class AuditLogger(ILogger<AuditLogger> logger) : IAuditLogger, Shared.Application.Auditing.IAuditLogger
{
    public Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        using (Serilog.Context.LogContext.PushProperty("Category", "Audit"))
        using (Serilog.Context.LogContext.PushProperty("UserId", entry.UserId))
        using (Serilog.Context.LogContext.PushProperty("Action", entry.Action))
        using (Serilog.Context.LogContext.PushProperty("Resource", entry.Resource))
        {
            logger.LogInformation(
                "Audit: {Action} on {Resource} (ID: {ResourceId}) by {UserName} - Success: {Success}",
                entry.Action, entry.Resource, entry.ResourceId, entry.UserName, entry.Success);
        }

        return Task.CompletedTask;
    }

    public async Task LogAsync(AuditLog auditLog, CancellationToken ct = default)
    {
        using (Serilog.Context.LogContext.PushProperty("Category", "Audit"))
        using (Serilog.Context.LogContext.PushProperty("UserId", auditLog.UserId))
        using (Serilog.Context.LogContext.PushProperty("Action", auditLog.Action))
        using (Serilog.Context.LogContext.PushProperty("Resource", auditLog.Resource))
        {
            logger.LogInformation(
                "Audit: {Action} on {Resource} (ID: {ResourceId}) by {UserName} - Success: {Success}",
                auditLog.Action,
                auditLog.Resource,
                auditLog.ResourceId,
                auditLog.UserName,
                auditLog.Success);
            
            await Task.CompletedTask;
        }
    }

    public async Task LogLoginAsync(string userId, string userName, string ipAddress, bool success, CancellationToken ct = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow.ToString("o"),
            Service = "AuthService",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            UserId = userId,
            UserName = userName,
            Action = "Login",
            Resource = "Auth",
            ResourceId = userId,
            IpAddress = ipAddress,
            Success = success
        };

        await LogAsync(auditLog, ct);
    }

    public async Task LogDataAccessAsync(string userId, string resource, string resourceId, string action, CancellationToken ct = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow.ToString("o"),
            Service = Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME") ?? "Unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            UserId = userId,
            UserName = userId,
            Action = action,
            Resource = resource,
            ResourceId = resourceId,
            Success = true
        };

        await LogAsync(auditLog, ct);
    }

    public async Task LogConfigurationChangeAsync(string userId, string setting, string oldValue, string newValue, CancellationToken ct = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow.ToString("o"),
            Service = Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME") ?? "Unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            UserId = userId,
            UserName = userId,
            Action = "ConfigurationChange",
            Resource = "Configuration",
            ResourceId = setting,
            Changes = new Dictionary<string, object>
            {
                ["OldValue"] = oldValue,
                ["NewValue"] = newValue
            },
            Success = true
        };

        await LogAsync(auditLog, ct);
    }
}
