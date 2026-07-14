namespace Notification.Domain.Entities;
public sealed class NotificationS
{
    private NotificationS() { }
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Recipient { get; private set; } = null!;
    public string Channel { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public NotificationStatus Status { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    public static NotificationS Create(string type, string recipient, string channel, string subject, string body, TimeProvider? timeProvider = null)
        => new() { Id=Guid.NewGuid(), Type=type, Recipient=recipient, Channel=channel,
                   Subject=subject, Body=body, Status=NotificationStatus.Pending, CreatedAt=(timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime };

    public void MarkSent(TimeProvider? timeProvider = null) { Status=NotificationStatus.Sent; SentAt=(timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime; }
    public void MarkFailed(string error, TimeProvider? timeProvider = null)
    {
        RetryCount++; Error=error;
        Status = RetryCount >= 3 ? NotificationStatus.DeadLetter : NotificationStatus.Failed;
    }
}
public enum NotificationStatus { Pending, Sent, Failed, DeadLetter }
