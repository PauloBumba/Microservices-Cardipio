namespace Shared.Infrastructure.Logging.Categories;

public class ApplicationLog
{
    public string Category => "Application";
    public string Level { get; set; }
    public string Message { get; set; }
    public string Service { get; set; }
    public string Environment { get; set; }
    public string TimestampUtc { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? MachineName { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
