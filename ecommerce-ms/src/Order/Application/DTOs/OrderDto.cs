namespace Order.Application.DTOs;

public sealed record OrderDto(
    Guid Id, string OrderNumber, Guid CustomerId,
    string Status, decimal Total, string Currency,
    List<OrderItemDto> Items, DateTime CreatedAt);

public sealed record OrderItemDto(
    Guid ProductId, string ProductName, string Sku,
    int Quantity, decimal UnitPrice, decimal Subtotal);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
