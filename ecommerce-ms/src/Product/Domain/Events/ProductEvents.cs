using Shared.Domain.Primitives;
namespace Product.Domain.Events;
public sealed record ProductCreatedDomainEvent(Guid ProductId, string Sku, string Name, TimeProvider? timeProvider = null) : DomainEvent(timeProvider);
public sealed record StockUpdatedDomainEvent(Guid ProductId, string Sku, int Stock, int Reserved, TimeProvider? timeProvider = null) : DomainEvent(timeProvider);
public sealed record ProductDeactivatedDomainEvent(Guid ProductId, TimeProvider? timeProvider = null) : DomainEvent(timeProvider);
