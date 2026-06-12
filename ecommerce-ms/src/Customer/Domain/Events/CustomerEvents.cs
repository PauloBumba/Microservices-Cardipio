using Customer.Domain.Primitives;
namespace Customer.Domain.Events;
public sealed record CustomerCreatedDomainEvent(Guid CustomerId, string Name, string Email) : IDomainEvent;
public sealed record CustomerDeactivatedDomainEvent(Guid CustomerId) : IDomainEvent;
