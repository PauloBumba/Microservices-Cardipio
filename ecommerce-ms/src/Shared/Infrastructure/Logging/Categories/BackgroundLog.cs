namespace Shared.Infrastructure.Logging.Categories;

public class BackgroundLog
{
    public string Category => "Background";
    public string TimestampUtc { get; set; }
    public string Service { get; set; }
    public string JobName { get; set; }
    public string Status { get; set; }  // Started, Completed, Failed
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public string? CorrelationId { get; set; }
}
