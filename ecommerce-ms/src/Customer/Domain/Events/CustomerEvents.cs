using Shared.Domain.Primitives;

namespace Customer.Domain.Events;

public sealed record CustomerCreatedDomainEvent(Guid CustomerId, string Name, string Email, TimeProvider? timeProvider = null) : DomainEvent(timeProvider);
public sealed record CustomerUpdatedDomainEvent(Guid CustomerId, string Name, TimeProvider? timeProvider = null) : DomainEvent(timeProvider);
public sealed record CustomerDeactivatedDomainEvent(Guid CustomerId, TimeProvider? timeProvider = null) : DomainEvent(timeProvider);
