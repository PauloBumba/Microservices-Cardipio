using System.Text.RegularExpressions;
using Customer.Domain.Exceptions;
namespace Customer.Domain.ValueObjects;

public sealed class Phone
{
    private static readonly Regex _r = new(@"^\+?[\d\s\-\(\)]{8,20}$", RegexOptions.Compiled);
    public string Value { get; }
    private Phone() => Value = string.Empty;
    private Phone(string value) => Value = value;
    public static Phone Create(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) throw new CustomerDomainException("Telefone obrigatório.");
        var n = phone.Trim();
        if (!_r.IsMatch(n)) throw new CustomerDomainException($"Telefone '{phone}' inválido.");
        return new Phone(n);
    }
    public override string ToString() => Value;
}