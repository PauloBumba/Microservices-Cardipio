namespace Shared.Infrastructure.Idempotency;

/// <summary>
/// Tabela de idempotência — evita processar o mesmo evento duas vezes.
/// Cada microsserviço deve ter sua própria tabela ProcessedEvents.
/// </summary>
public class ProcessedEvent
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = null!;
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
