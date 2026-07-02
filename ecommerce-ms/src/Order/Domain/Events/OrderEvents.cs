using Shared.Domain.Primitives;

namespace Order.Domain.Events
{
    public sealed record OrderCreatedDomainEvent(
        Guid OrderId,
        string OrderNumber,
        Guid CustomerId,
        decimal Total,
        string Currency,
        List<OrderItemSnapshot> Items) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }

    public sealed record OrderConfirmedDomainEvent(
        Guid OrderId,
        string OrderNumber) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }

    public sealed record OrderCancelledDomainEvent(
        Guid OrderId,
        string OrderNumber,
        Guid CustomerId,
        string Reason) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }

    public sealed record OrderItemSnapshot(
        Guid ProductId,
        string Sku,
        int Quantity,
        decimal UnitPrice);
}