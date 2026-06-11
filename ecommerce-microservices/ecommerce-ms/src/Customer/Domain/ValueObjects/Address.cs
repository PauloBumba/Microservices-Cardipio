using Customer.Domain.Exceptions;
namespace Customer.Domain.ValueObjects;
public sealed class Address
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Country { get; }
    private Address(string street, string city, string state, string zipCode, string country)
    { Street=street; City=city; State=state; ZipCode=zipCode; Country=country; }
    public static Address Create(string street, string city, string state, string zipCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))  throw new CustomerDomainException("Logradouro obrigatório.");
        if (string.IsNullOrWhiteSpace(city))    throw new CustomerDomainException("Cidade obrigatória.");
        if (string.IsNullOrWhiteSpace(state))   throw new CustomerDomainException("Estado obrigatório.");
        if (string.IsNullOrWhiteSpace(zipCode)) throw new CustomerDomainException("CEP obrigatório.");
        if (string.IsNullOrWhiteSpace(country)) throw new CustomerDomainException("País obrigatório.");
        return new Address(street.Trim(),city.Trim(),state.Trim(),zipCode.Trim(),country.Trim());
    }
}
