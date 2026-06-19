using Shared.Domain.Primitives;
namespace Order.Domain.Events;
public sealed record OrderCreatedDomainEvent(Guid OrderId, string OrderNumber, Guid CustomerId, decimal Total) : DomainEvent;
public sealed record OrderConfirmedDomainEvent(Guid OrderId, string OrderNumber) : DomainEvent;
public sealed record OrderCancelledDomainEvent(Guid OrderId, string OrderNumber, string Reason) : DomainEvent;
