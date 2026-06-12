using Order.Application.DTOs;
using Order.Domain.Entities;
namespace Order.Application.Mappings;
internal static class OrderMapper
{
    internal static OrderDto ToDto(this Orderss o) => new(
        o.Id, o.OrderNumber, o.CustomerId, o.Status.ToString(),
        new AddressDto(o.ShippingAddress.Street, o.ShippingAddress.City,
            o.ShippingAddress.State, o.ShippingAddress.ZipCode, o.ShippingAddress.Country),
        o.TotalAmount.Amount, o.TotalAmount.Currency, o.Notes,
        o.Items.Select(i => new OrderItemDto(
            i.Id, i.ProductId, i.ProductName, i.Sku,
            i.Quantity, i.UnitPrice, i.Currency, i.TotalPrice)).ToList(),
        o.CreatedAt, o.UpdatedAt);
}
