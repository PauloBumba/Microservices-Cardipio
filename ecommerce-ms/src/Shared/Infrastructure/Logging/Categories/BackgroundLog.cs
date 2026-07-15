namespace Shared.Infrastructure.Logging.Categories;

public class BackgroundLog
{
    public string Category => "Background";
    public string TimestampUtc { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;  // Started, Completed, Failed
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public string? CorrelationId { get; set; }
}
