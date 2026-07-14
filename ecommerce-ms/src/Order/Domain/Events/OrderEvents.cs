using Shared.Domain.Primitives;

namespace Order.Domain.Events
{
    public sealed record OrderCreatedDomainEvent(
        Guid OrderId,
        string OrderNumber,
        Guid CustomerId,
        decimal Total,
        string Currency,
        List<OrderItemSnapshot> Items,
        TimeProvider? timeProvider = null) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    }

    public sealed record OrderConfirmedDomainEvent(
        Guid OrderId,
        string OrderNumber,
        TimeProvider? timeProvider = null) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    }

    public sealed record OrderCancelledDomainEvent(
        Guid OrderId,
        string OrderNumber,
        Guid CustomerId,
        string Reason,
        TimeProvider? timeProvider = null) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    }

    public sealed record OrderItemSnapshot(
        Guid ProductId,
        string Sku,
        int Quantity,
        decimal UnitPrice);
}