using Order.Application.DTOs;
using Order.Domain.Entities;
namespace Order.Application.Mappings;
internal static class OrderMapper
{
    internal static OrderDto ToDto(this Orders o) => new(
        o.Id, o.OrderNumber, o.CustomerId, o.Status.ToString(),
        o.Total, o.Currency,
        o.Items.Select(i => new OrderItemDto(
            i.ProductId, i.ProductName, i.Sku,
            i.Quantity, i.UnitPrice, i.Subtotal)).ToList(),
        o.CreatedAt);
}
