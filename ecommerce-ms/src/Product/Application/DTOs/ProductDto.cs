namespace Product.Application.DTOs;

public sealed record ProductDto(
    Guid Id, string Name, string Description, string Sku,
    decimal Price, string Currency,
    int StockQuantity, int ReservedQuantity, int AvailableQuantity,
    string Category, bool IsActive,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record StockInfoDto(int StockQuantity, int ReservedQuantity, int AvailableQuantity);

public sealed record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);
