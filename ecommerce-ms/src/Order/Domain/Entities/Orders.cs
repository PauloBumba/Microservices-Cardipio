using Order.Domain.Events;
using Order.Domain.Exceptions;
using Shared.Domain.Primitives;

namespace Order.Domain.Entities;

public enum OrderStatus { Pending, Confirmed, Cancelled }

public sealed class Orders : AggregateRoot
{
    private readonly List<OrderItem> _items = [];
    private Orders() { }

    public string OrderNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Orders Create(Guid customerId, TimeProvider? timeProvider = null)
    {
        var provider = timeProvider ?? TimeProvider.System;
        var now = provider.GetUtcNow().UtcDateTime;

        var order = new Orders
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(provider),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            Total = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        return order;
    }

    public void AddItem(Guid productId, string productName, string sku, int qty, decimal unitPrice, string currency, TimeProvider? timeProvider = null)
    {
        if (Status != OrderStatus.Pending) throw new OrderDomainException("Não é possível adicionar itens a um pedido não pendente.");
        if (qty <= 0) throw new OrderDomainException("Quantidade deve ser positiva.");
        if (unitPrice <= 0) throw new OrderDomainException("Preço unitário deve ser positivo.");

        _items.Add(new OrderItem(Guid.NewGuid(), productId, productName, sku, qty, unitPrice));
        Total = _items.Sum(i => i.Subtotal);
        Currency = currency;
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    }

    public void Cancel(string reason, TimeProvider? timeProvider = null)
    {
        if (Status == OrderStatus.Cancelled) throw new OrderDomainException("Pedido já cancelado.");
        Status = OrderStatus.Cancelled;
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Raise(new OrderCancelledDomainEvent(Id, OrderNumber, CustomerId, reason, timeProvider));
    }

    public void Confirm(TimeProvider? timeProvider = null)
    {
        if (Status != OrderStatus.Pending) throw new OrderDomainException("Apenas pedidos pendentes podem ser confirmados.");
        if (_items.Count == 0) throw new OrderDomainException("Pedido sem itens não pode ser confirmado.");
        Status = OrderStatus.Confirmed;
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        Raise(new OrderConfirmedDomainEvent(Id, OrderNumber, timeProvider));
    }

    private static string GenerateOrderNumber(TimeProvider? timeProvider = null) =>
        $"ORD-{(timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
}
