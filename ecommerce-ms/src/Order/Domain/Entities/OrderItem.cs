using Order.Domain.Exceptions;
using Order.Domain.Primitives;
namespace Order.Domain.Entities;
public sealed class OrderItem : Entity
{
    private OrderItem() { }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = null!;
    public decimal TotalPrice => UnitPrice * Quantity;

    public static OrderItem Create(Guid orderId, Guid productId, string productName,
        string sku, int quantity, decimal unitPrice, string currency)
    {
        if (quantity <= 0) throw new OrderDomainException("Quantidade deve ser positiva.");
        if (unitPrice < 0) throw new OrderDomainException("Preço não pode ser negativo.");
        return new OrderItem
        {
            Id=Guid.NewGuid(), OrderId=orderId, ProductId=productId,
            ProductName=productName, Sku=sku, Quantity=quantity,
            UnitPrice=unitPrice, Currency=currency
        };
    }

    public OrderItem(Guid id, Guid productId, string productName, string sku, int qty, decimal unitPrice)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        Quantity = qty;
        UnitPrice = unitPrice;
    }

    public decimal Subtotal => UnitPrice * Quantity;
}
