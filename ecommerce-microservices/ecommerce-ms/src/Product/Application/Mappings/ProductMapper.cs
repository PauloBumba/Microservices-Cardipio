using Product.Application.DTOs;
using Product.Domain.Entities;
namespace Product.Application.Mappings;
internal static class ProductMapper
{
    internal static ProductDto ToDto(this Productss p) => new(
        p.Id, p.Name, p.Description, p.Sku, p.Price.Amount, p.Price.Currency,
        p.StockQuantity, p.ReservedQuantity, p.AvailableQuantity,
        p.Category, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
