using Customer.Application.DTOs;
using Customer.Domain.Entities;
namespace Customer.Application.Mappings;
internal static class CustomerMapper
{
    internal static CustomerDto ToDto(this Customerss c) => new(
        c.Id, c.Name, c.Email.Value, c.Phone.Value,
        new AddressDto(c.Address.Street, c.Address.City, c.Address.State, c.Address.ZipCode, c.Address.Country),
        c.IsActive, c.CreatedAt, c.UpdatedAt);
}
