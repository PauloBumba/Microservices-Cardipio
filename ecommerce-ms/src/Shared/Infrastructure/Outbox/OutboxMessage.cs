namespace Shared.Infrastructure.Outbox;

/// <summary>
/// Mensagem persistida no banco antes do despacho.
/// Cada microsserviço herda ou copia esta estrutura.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Type { get; init; } = null!;       // "Namespace.Classe, Assembly"
    public string Payload { get; init; } = null!;    // JSON serializado
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public bool IsProcessing { get; set; }
    public DateTime? LockedAt { get; set; }

    public static OutboxMessage Create(string type, string payload, TimeProvider timeProvider)
    {
        return new OutboxMessage
        {
            Type = type,
            Payload = payload,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
    }
}

public enum OutboxMessageStatus { Pending, Processed, DeadLetter }
