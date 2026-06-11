using Order.Domain.Exceptions;
namespace Order.Domain.ValueObjects;
public sealed class Address
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Country { get; }
    private Address(string street,string city,string state,string zipCode,string country)
    { Street=street; City=city; State=state; ZipCode=zipCode; Country=country; }
    public static Address Create(string street,string city,string state,string zipCode,string country)
    {
        if (string.IsNullOrWhiteSpace(street)) throw new OrderDomainException("Logradouro obrigatório.");
        if (string.IsNullOrWhiteSpace(city))   throw new OrderDomainException("Cidade obrigatória.");
        return new Address(street.Trim(),city.Trim(),state.Trim(),zipCode.Trim(),country.Trim());
    }
}
