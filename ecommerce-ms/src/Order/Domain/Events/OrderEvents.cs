using Order.Domain.Primitives;
namespace Order.Domain.Events;
public sealed record OrderItemSnapshot(Guid ProductId, string Sku, int Quantity, decimal UnitPrice);
public sealed record OrderCreatedDomainEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId,
    decimal TotalAmount, string Currency,
    IReadOnlyList<OrderItemSnapshot> Items) : IDomainEvent;
public sealed record OrderCancelledDomainEvent(
    Guid OrderId, string OrderNumber, Guid CustomerId, string Reason) : IDomainEvent;
