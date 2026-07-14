namespace Shared.Infrastructure.Logging.Categories;

public class SecurityLog
{
    public string Category => "Security";
    public string TimestampUtc { get; set; }
    public string Service { get; set; }
    public string Environment { get; set; }
    public string EventType { get; set; }  // Login, Logout, FailedAuth, PermissionDenied
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Resource { get; set; }
    public string? Details { get; set; }
    public string? CorrelationId { get; set; }
}
