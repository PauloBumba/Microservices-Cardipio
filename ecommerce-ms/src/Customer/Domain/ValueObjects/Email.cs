using System.Text.RegularExpressions;
using Customer.Domain.Exceptions;
namespace Customer.Domain.ValueObjects;

public sealed class Email
{
    private static readonly Regex _r = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public string Value { get; }
    private Email() => Value = string.Empty; // construtor para o EF
    private Email(string value) => Value = value;
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new CustomerDomainException("E-mail obrigatório.");
        var n = email.Trim().ToLowerInvariant();
        if (!_r.IsMatch(n)) throw new CustomerDomainException($"E-mail '{email}' inválido.");
        return new Email(n);
    }
    public override string ToString() => Value;
    public override bool Equals(object? o) => o is Email e && Value == e.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator string(Email e) => e.Value;
}