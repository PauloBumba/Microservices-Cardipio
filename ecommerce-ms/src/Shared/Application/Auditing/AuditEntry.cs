namespace Shared.Application.Auditing;

public sealed class AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string Service { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string? ResourceId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CorrelationId { get; init; }
    public string? TraceId { get; init; }
}
