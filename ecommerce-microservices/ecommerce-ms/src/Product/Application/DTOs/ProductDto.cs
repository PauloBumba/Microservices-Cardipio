namespace Product.Application.DTOs;
public sealed record ProductDto(Guid Id, string Name, string Description, string Sku,
    decimal Price, string Currency, int StockQuantity, int ReservedQuantity, int AvailableQuantity,
    string Category, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record StockInfoDto(Guid ProductId, string Sku, int StockQuantity, int ReservedQuantity, int AvailableQuantity);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
