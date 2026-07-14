using Customer.Domain.Events;
using Customer.Domain.Exceptions;
using Customer.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customer.Domain.Entities;

public sealed class Customerss : AggregateRoot
{
    private Customerss() { }

    public string Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Phone Phone { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Customerss Create(string name, string email, string phone,
        string street, string city, string state, string zipCode, string country,
        TimeProvider? timeProvider = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new CustomerDomainException("Nome obrigatório.");

        var provider = timeProvider ?? TimeProvider.System;
        var now = provider.GetUtcNow().UtcDateTime;

        var c = new Customerss
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = Email.Create(email),
            Phone = Phone.Create(phone),
            Address = Address.Create(street, city, state, zipCode, country),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        c.Raise(new CustomerCreatedDomainEvent(c.Id, c.Name, c.Email.Value, provider));
        return c;
    }

    public void Update(string name, string phone,
        string street, string city, string state, string zipCode, string country,
        TimeProvider? timeProvider = null)
    {
        if (!IsActive) throw new CustomerDomainException("Cliente inativo não pode ser atualizado.");
        Name = name.Trim();
        Phone = Phone.Create(phone);
        Address = Address.Create(street, city, state, zipCode, country);
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Raise(new CustomerUpdatedDomainEvent(Id, Name, timeProvider));
    }

    public void Deactivate(TimeProvider? timeProvider = null)
    {
        if (!IsActive) throw new CustomerDomainException("Cliente já inativo.");
        IsActive = false;
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Raise(new CustomerDeactivatedDomainEvent(Id, timeProvider));
    }
}
