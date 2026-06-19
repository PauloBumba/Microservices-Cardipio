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

    public static Orders Create(Guid customerId)
    {
        var order = new Orders
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            Total = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return order;
    }

    public void AddItem(Guid productId, string productName, string sku, int qty, decimal unitPrice, string currency)
    {
        if (Status != OrderStatus.Pending) throw new OrderDomainException("Não é possível adicionar itens a um pedido não pendente.");
        if (qty <= 0) throw new OrderDomainException("Quantidade deve ser positiva.");
        if (unitPrice <= 0) throw new OrderDomainException("Preço unitário deve ser positivo.");

        _items.Add(new OrderItem(Guid.NewGuid(), productId, productName, sku, qty, unitPrice));
        Total = _items.Sum(i => i.Subtotal);
        Currency = currency;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending) throw new OrderDomainException("Apenas pedidos pendentes podem ser confirmados.");
        if (_items.Count == 0) throw new OrderDomainException("Pedido sem itens não pode ser confirmado.");
        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        Raise(new OrderConfirmedDomainEvent(Id, OrderNumber));
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Cancelled) throw new OrderDomainException("Pedido já cancelado.");
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        Raise(new OrderCancelledDomainEvent(Id, OrderNumber, reason));
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
}

public sealed class OrderItem
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string Sku { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal => Quantity * UnitPrice;

    public OrderItem(Guid id, Guid productId, string productName, string sku, int qty, decimal unitPrice)
    {
        Id = id; ProductId = productId; ProductName = productName;
        Sku = sku; Quantity = qty; UnitPrice = unitPrice;
    }
    private OrderItem() { }
}
