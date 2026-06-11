using Customer.Domain.Events;
using Customer.Domain.Exceptions;
using Customer.Domain.Primitives;
using Customer.Domain.ValueObjects;

namespace Customer.Domain.Entities
{
    public sealed class Customerss : Entity
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
            string street, string city, string state, string zipCode, string country)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new CustomerDomainException("Nome obrigatório.");
            var c = new Customerss
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Email = Email.Create(email),
                Phone = Phone.Create(phone),
                Address = Address.Create(street, city, state, zipCode, country),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            c.Raise(new CustomerCreatedDomainEvent(c.Id, c.Name, c.Email.Value));
            return c;
        }

        public void Update(string name, string phone, string street, string city, string state, string zipCode, string country)
        {
            if (!IsActive) throw new CustomerDomainException("Cliente inativo não pode ser atualizado.");
            Name = name.Trim(); Phone = Phone.Create(phone);
            Address = Address.Create(street, city, state, zipCode, country);
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive) throw new CustomerDomainException("Cliente já inativo.");
            IsActive = false; UpdatedAt = DateTime.UtcNow;
            Raise(new CustomerDeactivatedDomainEvent(Id));
        }
    }
}