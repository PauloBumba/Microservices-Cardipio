namespace Shared.Infrastructure.Logging.Categories;

public class AuditLog
{
    public string Category => "Audit";
    public Guid Id { get; set; }
    public string TimestampUtc { get; set; }
    public string Service { get; set; }
    public string Environment { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Action { get; set; }
    public string Resource { get; set; }
    public string ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Changes { get; set; } = new();
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
}
