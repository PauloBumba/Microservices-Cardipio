namespace Order.Application.DTOs;
public sealed record OrderDto(
    Guid Id, string OrderNumber, Guid CustomerId, string Status,
    AddressDto ShippingAddress, decimal TotalAmount, string Currency, string? Notes,
    IReadOnlyList<OrderItemDto> Items, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record OrderItemDto(
    Guid Id, Guid ProductId, string ProductName, string Sku,
    int Quantity, decimal UnitPrice, string Currency, decimal TotalPrice);
public sealed record AddressDto(string Street, string City, string State, string ZipCode, string Country);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
