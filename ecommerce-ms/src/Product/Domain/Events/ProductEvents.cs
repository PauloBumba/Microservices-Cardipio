using Shared.Domain.Primitives;
namespace Product.Domain.Events;
public sealed record ProductCreatedDomainEvent(Guid ProductId, string Sku, string Name) : DomainEvent;
public sealed record StockUpdatedDomainEvent(Guid ProductId, string Sku, int Stock, int Reserved) : DomainEvent;
public sealed record ProductDeactivatedDomainEvent(Guid ProductId) : DomainEvent;
