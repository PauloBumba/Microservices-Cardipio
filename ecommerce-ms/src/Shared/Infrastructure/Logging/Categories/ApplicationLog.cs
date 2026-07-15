namespace Shared.Infrastructure.Logging.Categories;

public class ApplicationLog
{
    public string Category => "Application";
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string TimestampUtc { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? MachineName { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
