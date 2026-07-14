namespace Shared.Infrastructure.Idempotency;

/// <summary>
/// Tabela de idempotência — evita processar o mesmo evento duas vezes.
/// Cada microsserviço deve ter sua própria tabela ProcessedEvents.
/// </summary>
public class ProcessedEvent
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = null!;
    public DateTime ProcessedAt { get; init; }

    public static ProcessedEvent Create(Guid eventId, string eventType, TimeProvider timeProvider)
    {
        return new ProcessedEvent
        {
            EventId = eventId,
            EventType = eventType,
            ProcessedAt = timeProvider.GetUtcNow().UtcDateTime
        };
    }
}
