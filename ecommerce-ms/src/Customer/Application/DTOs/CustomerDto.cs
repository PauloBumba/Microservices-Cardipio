namespace Customer.Application.DTOs;
public sealed record CustomerDto(Guid Id, string Name, string Email, string Phone, AddressDto Address, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record AddressDto(string Street, string City, string State, string ZipCode, string Country);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
}
