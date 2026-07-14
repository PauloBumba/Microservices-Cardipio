namespace Shared.Domain.Primitives;
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; }

    protected DomainEvent(TimeProvider? timeProvider = null)
    {
        var provider = timeProvider ?? TimeProvider.System;
        OccurredAt = provider.GetUtcNow().UtcDateTime;
    }
}
