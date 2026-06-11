using Order.Domain.Enums;
using Order.Domain.Events;
using Order.Domain.Exceptions;
using Order.Domain.Primitives;
using Order.Domain.ValueObjects;
namespace Order.Domain.Entities;
public sealed class Orderss : Entity
{
    private readonly List<OrderItem> _items = [];
    private Orderss() { }
    public string OrderNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Address ShippingAddress { get; private set; } = null!;
    public Money TotalAmount { get; private set; } = null!;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Orderss Create(Guid customerId, string street, string city,
        string state, string zipCode, string country, string? notes=null)
    {
        var order = new Orderss
        {
            Id=Guid.NewGuid(),
            OrderNumber=$"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            CustomerId=customerId,
            Status=OrderStatus.Pending,
            ShippingAddress=Address.Create(street,city,state,zipCode,country),
            TotalAmount=Money.Zero("BRL"),
            Notes=notes,
            CreatedAt=DateTime.UtcNow,
            UpdatedAt=DateTime.UtcNow
        };
        return order;
    }

    public void AddItem(Guid productId, string productName, string sku,
        int quantity, decimal unitPrice, string currency)
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException("Só é possível adicionar itens a pedidos pendentes.");
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null) _items.Remove(existing);
        _items.Add(OrderItem.Create(Id, productId, productName, sku, quantity, unitPrice, currency));
        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException($"Pedido em '{Status}' não pode ser confirmado.");
        if (_items.Count == 0)
            throw new OrderDomainException("Pedido sem itens não pode ser confirmado.");
        Status=OrderStatus.Confirmed;
        UpdatedAt=DateTime.UtcNow;
        Raise(new OrderCreatedDomainEvent(
            Id, OrderNumber, CustomerId, TotalAmount.Amount, TotalAmount.Currency,
            _items.Select(i => new OrderItemSnapshot(i.ProductId,i.Sku,i.Quantity,i.UnitPrice)).ToList()));
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new OrderDomainException("Pedido enviado/entregue não pode ser cancelado.");
        Status=OrderStatus.Cancelled;
        Notes=$"Cancelado: {reason}";
        UpdatedAt=DateTime.UtcNow;
        Raise(new OrderCancelledDomainEvent(Id, OrderNumber, CustomerId, reason));
    }

    private void RecalculateTotal()
    {
        var currency = _items.FirstOrDefault()?.Currency ?? "BRL";
        TotalAmount = Money.Create(_items.Sum(i => i.TotalPrice), currency);
    }
}
