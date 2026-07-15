namespace Shared.Infrastructure.Logging.Categories;

public class AuditLog
{
    public string Category => "Audit";
    public Guid Id { get; set; }
    public string TimestampUtc { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Changes { get; set; } = new();
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
}
