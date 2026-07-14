using Product.Domain.Events;
using Product.Domain.Exceptions;
using Product.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Product.Domain.Entities;

public sealed class Productss : AggregateRoot
{
    private Productss() { }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int StockQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public string Category { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int AvailableQuantity => StockQuantity - ReservedQuantity;

    public static Productss Create(string name, string description, string sku,
        decimal price, string currency, int initialStock, string category,
        TimeProvider? timeProvider = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ProductDomainException("Nome obrigatório.");
        if (string.IsNullOrWhiteSpace(sku))  throw new ProductDomainException("SKU obrigatório.");
        if (initialStock < 0)                throw new ProductDomainException("Estoque não pode ser negativo.");

        var provider = timeProvider ?? TimeProvider.System;
        var now = provider.GetUtcNow().UtcDateTime;

        var p = new Productss
        {
            Id = Guid.NewGuid(), Name = name.Trim(), Description = description?.Trim() ?? string.Empty,
            Sku = sku.Trim().ToUpperInvariant(), Price = Money.Create(price, currency),
            StockQuantity = initialStock, ReservedQuantity = 0, Category = category.Trim(),
            IsActive = true, CreatedAt = now, UpdatedAt = now
        };
        p.Raise(new ProductCreatedDomainEvent(p.Id, p.Sku, p.Name, provider));
        return p;
    }

    public void AddStock(int qty, TimeProvider? timeProvider = null)
    {
        if (qty <= 0) throw new ProductDomainException("Quantidade deve ser positiva.");
        StockQuantity += qty; UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Raise(new StockUpdatedDomainEvent(Id, Sku, StockQuantity, ReservedQuantity, timeProvider));
    }

    public void ReserveStock(int qty, TimeProvider? timeProvider = null)
    {
        if (!IsActive) throw new ProductDomainException("Produto inativo.");
        if (qty <= 0) throw new ProductDomainException("Quantidade deve ser positiva.");
        if (AvailableQuantity < qty) throw new InsufficientStockException(Id, qty, AvailableQuantity);
        ReservedQuantity += qty; UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Raise(new StockUpdatedDomainEvent(Id, Sku, StockQuantity, ReservedQuantity, timeProvider));
    }

    public void ConfirmReservation(int qty, TimeProvider? timeProvider = null)
    {
        if (ReservedQuantity < qty) throw new ProductDomainException("Reserva insuficiente.");
        StockQuantity -= qty; ReservedQuantity -= qty; UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Raise(new StockUpdatedDomainEvent(Id, Sku, StockQuantity, ReservedQuantity, timeProvider));
    }

    public void Deactivate(TimeProvider? timeProvider = null)
    {
        IsActive = false; UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Raise(new ProductDeactivatedDomainEvent(Id, timeProvider));
    }
}
