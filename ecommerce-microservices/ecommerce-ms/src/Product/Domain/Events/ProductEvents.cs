using Product.Domain.Primitives;
namespace Product.Domain.Events;
public sealed record ProductCreatedDomainEvent(Guid ProductId, string Sku, string Name) : IDomainEvent;
public sealed record StockUpdatedDomainEvent(Guid ProductId, string Sku, int NewStock, int Reserved) : IDomainEvent;
