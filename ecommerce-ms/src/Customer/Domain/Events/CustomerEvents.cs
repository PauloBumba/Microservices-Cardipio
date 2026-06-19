using Shared.Domain.Primitives;

namespace Customer.Domain.Events;

public sealed record CustomerCreatedDomainEvent(Guid CustomerId, string Name, string Email) : DomainEvent;
public sealed record CustomerUpdatedDomainEvent(Guid CustomerId, string Name) : DomainEvent;
public sealed record CustomerDeactivatedDomainEvent(Guid CustomerId) : DomainEvent;
